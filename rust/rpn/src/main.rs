mod eval;
mod ops;
mod style;

use std::io::Write;

use eval::{Engine, Step, fmt_num, fmt_stack};
use style::Style;

fn main() {
    let mut args: Vec<String> = std::env::args().skip(1).collect();

    let trace = matches!(args.first().map(String::as_str), Some("-t" | "--trace"));
    if trace {
        args.remove(0);
    }

    let style = Style::detect();

    match args.first().map(String::as_str) {
        Some("-h" | "--help") => print_help(style),
        Some(_) => one_shot(&args.join(" "), trace, style),
        None => repl(trace, style),
    }
}

fn one_shot(expression: &str, trace: bool, style: Style) {
    let mut engine = Engine::new();

    let result = if trace {
        engine
            .eval_traced(expression)
            .map(|steps| print_trace(&steps, style))
    } else {
        engine.eval_line(expression)
    };

    match result {
        Ok(()) => {
            if let Some(&top) = engine.stack().last() {
                println!("{}", fmt_num(top));
            }
        }
        Err(error) => {
            eprintln!("{} {error}", style.red("erreur :"));
            std::process::exit(1);
        }
    }
}

fn repl(mut trace: bool, style: Style) {
    println!(
        "{} {}",
        style.bold("blap"),
        style.dim("— calculatrice RPN. `?` pour l'aide, `q` pour quitter.")
    );

    let mut engine = Engine::new();
    let mut line = String::new();

    loop {
        print_prompt(&engine, style);

        line.clear();
        match std::io::stdin().read_line(&mut line) {
            Ok(0) => {
                println!();
                break;
            }
            Ok(_) => {}
            Err(error) => {
                eprintln!("{} {error}", style.red("lecture impossible :"));
                break;
            }
        }

        match line.trim() {
            "" => continue,
            "q" | "quit" | "exit" => break,
            "?" | "help" => print_help(style),
            "trace" => {
                trace = !trace;
                let state = if trace { "activée" } else { "coupée" };
                println!("  {}", style.dim(&format!("trace {state}")));
            }
            input => {
                let result = if trace {
                    engine
                        .eval_traced(input)
                        .map(|steps| print_trace(&steps, style))
                } else {
                    engine.eval_line(input)
                };

                match result {
                    Ok(()) => match engine.stack().last() {
                        Some(&top) => {
                            println!("  {} {}", style.dim("="), style.green(&fmt_num(top)));
                        }
                        None => println!("  {}", style.dim("(pile vide)")),
                    },
                    Err(error) => eprintln!("  {} {error}", style.red("×")),
                }
            }
        }
    }

    println!("{}", style.dim("à bientôt"));
}

fn print_prompt(engine: &Engine, style: Style) {
    let stack = engine.stack();

    if stack.is_empty() {
        print!("{} ", style.cyan("rpn ›"));
    } else {
        let shown = format!("[{}]", fmt_stack(stack));
        print!("{} {} ", style.dim(&shown), style.cyan("›"));
    }

    let _ = std::io::stdout().flush();
}

fn print_trace(steps: &[Step], style: Style) {
    for step in steps {
        let token = format!("{:<6}", step.token);
        let after = format!("→ [{}]", fmt_stack(&step.after));
        println!(
            "  {} {:<24} {}",
            style.cyan(&token),
            explain(&step.token, &step.before, &step.after),
            style.dim(&after)
        );
    }
}

fn explain(token: &str, before: &[f64], after: &[f64]) -> String {
    if token.parse::<f64>().is_ok() {
        return format!("empile {token}");
    }

    match token {
        "dup" => String::from("duplique le sommet"),
        "drop" => String::from("retire le sommet"),
        "swap" => String::from("échange les deux du sommet"),
        "over" => String::from("copie l'avant-dernier au sommet"),
        "rot" => String::from("fait tourner les trois du sommet"),
        "clear" | "cls" => String::from("vide la pile"),
        "sum" => format!("somme de la pile = {}", fmt_num(top(after))),
        "prod" => format!("produit de la pile = {}", fmt_num(top(after))),
        _ if before.len() == after.len() + 1 && before.len() >= 2 => {
            let a = fmt_num(before[before.len() - 2]);
            let b = fmt_num(before[before.len() - 1]);
            let result = fmt_num(top(after));
            match infix_symbol(token) {
                Some(symbol) => format!("{a} {symbol} {b} = {result}"),
                None => format!("{token}({a}, {b}) = {result}"),
            }
        }
        _ if before.len() == after.len() && !before.is_empty() => {
            format!(
                "{token}({}) = {}",
                fmt_num(top(before)),
                fmt_num(top(after))
            )
        }
        _ if after.len() == before.len() + 1 => {
            format!("empile {token} = {}", fmt_num(top(after)))
        }
        _ => token.to_string(),
    }
}

fn infix_symbol(token: &str) -> Option<&str> {
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

fn print_help(style: Style) {
    println!();
    println!("{}", style.bold("blap — notation polonaise inverse (RPN)"));
    println!();
    println!("{}", style.yellow("Saisie"));
    println!("  Empile les nombres puis applique l'opérateur : `3 4 + 2 *` donne 14");
    println!("  Décimaux, négatifs et notation scientifique acceptés : `-1.5e3`");
    println!();
    println!("{}", style.yellow("Opérateurs"));
    println!("  +  -  *  /          arithmétique");
    println!("  %  mod              modulo             ^  **  pow        puissance");
    println!("  neg                 opposé             abs               valeur absolue");
    println!("  sqrt                racine             inv               inverse 1/x");
    println!("  ln  log  log2       logarithmes        exp               e^x");
    println!("  sin  cos  tan       trigo (radians)    floor ceil round  arrondis");
    println!("  min  max            extrema            !  fact           factorielle");
    println!();
    println!("{}", style.yellow("Pile"));
    println!("  dup drop swap over rot                 manipulation");
    println!("  clear               vide la pile       sum  prod         réduit la pile");
    println!();
    println!("{}", style.yellow("Constantes"));
    println!("  pi  e  tau");
    println!();
    println!("{}", style.yellow("Commandes"));
    println!("  ?  help             aide               q  quit  exit     quitter");
    println!("  trace               décompose le calcul pas à pas (ou l'option -t)");
    println!();
}
