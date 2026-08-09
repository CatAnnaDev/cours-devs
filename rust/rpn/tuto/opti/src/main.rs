use std::io::{BufWriter, Write};

use blap_opti::eval::{Engine, Num, Trace, write_stack};
use blap_opti::style::Style;

fn main() {
    let mut args: Vec<String> = std::env::args().skip(1).collect();

    let trace = matches!(args.first().map(String::as_str), Some("-t" | "--trace"));
    if trace {
        args.remove(0);
    }

    let style = Style::detect();

    match args.first().map(String::as_str) {
        Some("-h" | "--help") => {
            let mut out = BufWriter::new(std::io::stdout().lock());
            print_help(&mut out, style);
            let _ = out.flush();
        }
        Some(_) => one_shot(&args.join(" "), trace, style),
        None => repl(trace, style),
    }
}

fn one_shot(expression: &str, trace: bool, style: Style) {
    let mut engine = Engine::new();
    let mut out = BufWriter::new(std::io::stdout().lock());
    let mut scratch = String::new();
    let mut trace_buffer = Trace::new();

    let result = if trace {
        engine
            .eval_traced(expression, &mut trace_buffer)
            .map(|()| print_trace(&mut out, &trace_buffer, expression, &mut scratch, style))
    } else {
        engine.eval_line(expression)
    };

    match result {
        Ok(()) => {
            if let Some(&top) = engine.stack().last() {
                let _ = writeln!(out, "{}", Num(top));
            }
            let _ = out.flush();
        }
        Err(error) => {
            let _ = out.flush();
            eprintln!("{} {error}", style.red("erreur :"));
            std::process::exit(1);
        }
    }
}

fn repl(mut trace: bool, style: Style) {
    let mut out = std::io::stdout().lock();
    let mut engine = Engine::new();
    let mut line = String::new();
    let mut scratch = String::new();
    let mut trace_buffer = Trace::new();

    let _ = writeln!(
        out,
        "{} {}",
        style.bold("blap"),
        style.dim("— calculatrice RPN. `?` pour l'aide, `q` pour quitter.")
    );

    loop {
        print_prompt(&mut out, &engine, &mut scratch, style);

        line.clear();
        match std::io::stdin().read_line(&mut line) {
            Ok(0) => {
                let _ = writeln!(out);
                break;
            }
            Ok(_) => {}
            Err(error) => {
                let _ = out.flush();
                eprintln!("{} {error}", style.red("lecture impossible :"));
                break;
            }
        }

        match line.trim() {
            "" => continue,
            "q" | "quit" | "exit" => break,
            "?" | "help" => print_help(&mut out, style),
            "trace" => {
                trace = !trace;
                let state = if trace { "activée" } else { "coupée" };
                scratch.clear();
                scratch.push_str("trace ");
                scratch.push_str(state);
                let _ = writeln!(out, "  {}", style.dim(&scratch));
            }
            input => {
                let result = if trace {
                    engine
                        .eval_traced(input, &mut trace_buffer)
                        .map(|()| print_trace(&mut out, &trace_buffer, input, &mut scratch, style))
                } else {
                    engine.eval_line(input)
                };

                match result {
                    Ok(()) => match engine.stack().last() {
                        Some(&top) => {
                            scratch.clear();
                            write_stack(&mut scratch, &[top]);
                            let _ = writeln!(out, "  {} {}", style.dim("="), style.green(&scratch));
                        }
                        None => {
                            let _ = writeln!(out, "  {}", style.dim("(pile vide)"));
                        }
                    },
                    Err(error) => {
                        let _ = out.flush();
                        eprintln!("  {} {error}", style.red("×"));
                    }
                }
            }
        }
    }

    let _ = writeln!(out, "{}", style.dim("à bientôt"));
}

fn print_prompt(out: &mut impl Write, engine: &Engine, scratch: &mut String, style: Style) {
    let stack = engine.stack();

    if stack.is_empty() {
        let _ = write!(out, "{} ", style.cyan("rpn ›"));
    } else {
        scratch.clear();
        scratch.push('[');
        write_stack(scratch, stack);
        scratch.push(']');
        let _ = write!(out, "{} {} ", style.dim(scratch), style.cyan("›"));
    }

    let _ = out.flush();
}

fn print_trace(
    out: &mut impl Write,
    trace: &Trace,
    line: &str,
    scratch: &mut String,
    style: Style,
) {
    let mut column = String::new();

    for step in trace.iter(line) {
        column.clear();
        pad(&mut column, step.token, 6);

        scratch.clear();
        explain(scratch, step.token, step.before, step.after);
        let _ = write!(out, "  {} {:<24}", style.cyan(&column), scratch);

        column.clear();
        column.push_str("→ [");
        write_stack(&mut column, step.after);
        column.push(']');
        let _ = writeln!(out, " {}", style.dim(&column));
    }
}

fn pad(out: &mut String, text: &str, width: usize) {
    out.push_str(text);
    for _ in text.chars().count()..width {
        out.push(' ');
    }
}

fn explain(out: &mut String, token: &str, before: &[f64], after: &[f64]) {
    use std::fmt::Write;

    if token.parse::<f64>().is_ok() {
        let _ = write!(out, "empile {token}");
        return;
    }

    match token {
        "dup" => out.push_str("duplique le sommet"),
        "drop" => out.push_str("retire le sommet"),
        "swap" => out.push_str("échange les deux du sommet"),
        "over" => out.push_str("copie l'avant-dernier au sommet"),
        "rot" => out.push_str("fait tourner les trois du sommet"),
        "clear" | "cls" => out.push_str("vide la pile"),
        "sum" => {
            let _ = write!(out, "somme de la pile = {}", Num(top(after)));
        }
        "prod" => {
            let _ = write!(out, "produit de la pile = {}", Num(top(after)));
        }
        _ if before.len() == after.len() + 1 && before.len() >= 2 => {
            let a = Num(before[before.len() - 2]);
            let b = Num(before[before.len() - 1]);
            let result = Num(top(after));
            let _ = match infix_symbol(token) {
                Some(symbol) => write!(out, "{a} {symbol} {b} = {result}"),
                None => write!(out, "{token}({a}, {b}) = {result}"),
            };
        }
        _ if before.len() == after.len() && !before.is_empty() => {
            let _ = write!(out, "{token}({}) = {}", Num(top(before)), Num(top(after)));
        }
        _ if after.len() == before.len() + 1 => {
            let _ = write!(out, "empile {token} = {}", Num(top(after)));
        }
        _ => out.push_str(token),
    }
}

fn infix_symbol(token: &str) -> Option<&'static str> {
    match token {
        "+" => Some("+"),
        "-" => Some("−"),
        "*" => Some("×"),
        "/" => Some("÷"),
        "^" | "**" | "pow" => Some("^"),
        "%" | "mod" => Some("mod"),
        _ => None,
    }
}

fn top(stack: &[f64]) -> f64 {
    match stack.last() {
        Some(&n) => n,
        None => 0.0,
    }
}

fn print_help(out: &mut impl Write, style: Style) {
    let _ = writeln!(out);
    let _ = writeln!(
        out,
        "{}",
        style.bold("blap — notation polonaise inverse (RPN)")
    );
    let _ = writeln!(out);
    let _ = writeln!(out, "{}", style.yellow("Saisie"));
    let _ = writeln!(
        out,
        "  Empile les nombres puis applique l'opérateur : `3 4 + 2 *` donne 14"
    );
    let _ = writeln!(
        out,
        "  Décimaux, négatifs et notation scientifique acceptés : `-1.5e3`"
    );
    let _ = writeln!(out);
    let _ = writeln!(out, "{}", style.yellow("Opérateurs"));
    let _ = writeln!(out, "  +  -  *  /          arithmétique");
    let _ = writeln!(
        out,
        "  %  mod              modulo             ^  **  pow        puissance"
    );
    let _ = writeln!(
        out,
        "  neg                 opposé             abs               valeur absolue"
    );
    let _ = writeln!(
        out,
        "  sqrt                racine             inv               inverse 1/x"
    );
    let _ = writeln!(
        out,
        "  ln  log  log2       logarithmes        exp               e^x"
    );
    let _ = writeln!(
        out,
        "  sin  cos  tan       trigo (radians)    floor ceil round  arrondis"
    );
    let _ = writeln!(
        out,
        "  min  max            extrema            !  fact           factorielle"
    );
    let _ = writeln!(out);
    let _ = writeln!(out, "{}", style.yellow("Pile"));
    let _ = writeln!(out, "  dup drop swap over rot                 manipulation");
    let _ = writeln!(
        out,
        "  clear               vide la pile       sum  prod         réduit la pile"
    );
    let _ = writeln!(out);
    let _ = writeln!(out, "{}", style.yellow("Constantes"));
    let _ = writeln!(out, "  pi  e  tau");
    let _ = writeln!(out);
    let _ = writeln!(out, "{}", style.yellow("Commandes"));
    let _ = writeln!(
        out,
        "  ?  help             aide               q  quit  exit     quitter"
    );
    let _ = writeln!(
        out,
        "  trace               décompose le calcul pas à pas (ou l'option -t)"
    );
    let _ = writeln!(out);
}
