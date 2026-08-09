use std::fmt::Write as FmtWrite;
use std::hint::black_box;
use std::time::{Duration, Instant};

use blap_opti::eval::{Engine, Trace, write_stack};
use blap_opti::naive;

const LINES: [&str; 6] = [
    "5 1 2 + 4 * + 3 -",
    "3 dup * 4 dup * + sqrt",
    "1 2 3 4 5 sum",
    "2 10 pow 2 log2",
    "pi 2 / sin 1 +",
    "10 3 % 7 max 2 min",
];

const STACK: [f64; 8] = [1.0, -2.5, 3.0, 1e-3, 42.0, -0.0, 1234.5, 7.0];

const ROUNDS: u32 = 7;
const BATCH: u32 = 20_000;

fn measure(mut body: impl FnMut()) -> Duration {
    body();
    let mut best = Duration::MAX;
    for _ in 0..ROUNDS {
        let start = Instant::now();
        body();
        let elapsed = start.elapsed();
        if elapsed < best {
            best = elapsed;
        }
    }
    best
}

fn report(label: &str, operations: u64, naive: Duration, fast: Duration) {
    let ns = |d: Duration| d.as_secs_f64() * 1e9 / operations as f64;
    let (a, b) = (ns(naive), ns(fast));
    println!("{label:<34} {a:>9.1} ns {b:>9.1} ns   ×{:.2}", a / b);
}

fn bench_eval() {
    let operations = u64::from(BATCH) * LINES.len() as u64;

    let naive = measure(|| {
        let mut engine = naive::Engine::new();
        for _ in 0..BATCH {
            for line in LINES {
                engine.eval_line(black_box(line)).unwrap();
            }
            black_box(engine.stack());
            engine.eval_line("clear").unwrap();
        }
    });

    let fast = measure(|| {
        let mut engine = Engine::new();
        for _ in 0..BATCH {
            for line in LINES {
                engine.eval_line(black_box(line)).unwrap();
            }
            black_box(engine.stack());
            engine.eval_line("clear").unwrap();
        }
    });

    report("evaluation d'une ligne", operations, naive, fast);
}

fn bench_eval_errors() {
    let operations = u64::from(BATCH) * 2;

    let naive = measure(|| {
        let mut engine = naive::Engine::new();
        for _ in 0..BATCH {
            black_box(engine.eval_line(black_box("1 2 3 oups +"))).ok();
            black_box(engine.eval_line(black_box("1 2 + +"))).ok();
        }
    });

    let fast = measure(|| {
        let mut engine = Engine::new();
        for _ in 0..BATCH {
            black_box(engine.eval_line(black_box("1 2 3 oups +"))).ok();
            black_box(engine.eval_line(black_box("1 2 + +"))).ok();
        }
    });

    report("ligne qui echoue", operations, naive, fast);
}

fn bench_trace() {
    let operations = u64::from(BATCH) * LINES.len() as u64;

    let naive = measure(|| {
        let mut engine = naive::Engine::new();
        let mut rendered = String::new();
        for _ in 0..BATCH {
            for line in LINES {
                let steps = engine.eval_traced(black_box(line)).unwrap();
                rendered.clear();
                for step in &steps {
                    rendered.push_str(&step.token);
                    rendered.push_str(&naive::fmt_stack(&step.after));
                }
                black_box(&rendered);
            }
            engine.eval_line("clear").unwrap();
        }
    });

    let fast = measure(|| {
        let mut engine = Engine::new();
        let mut trace = Trace::new();
        let mut rendered = String::new();
        for _ in 0..BATCH {
            for line in LINES {
                engine.eval_traced(black_box(line), &mut trace).unwrap();
                rendered.clear();
                for step in trace.iter(line) {
                    rendered.push_str(step.token);
                    write_stack(&mut rendered, step.after);
                }
                black_box(&rendered);
            }
            engine.eval_line("clear").unwrap();
        }
    });

    report("trace + rendu d'une ligne", operations, naive, fast);
}

fn bench_format() {
    let operations = u64::from(BATCH) * 10;

    let naive = measure(|| {
        for _ in 0..BATCH {
            for _ in 0..10 {
                black_box(naive::fmt_stack(black_box(&STACK)));
            }
        }
    });

    let fast = measure(|| {
        let mut buffer = String::new();
        for _ in 0..BATCH {
            for _ in 0..10 {
                buffer.clear();
                write_stack(&mut buffer, black_box(&STACK));
                black_box(&buffer);
            }
        }
    });

    report("formatage d'une pile de 8", operations, naive, fast);
}

fn bench_prompt() {
    let operations = u64::from(BATCH) * 10;

    let naive = measure(|| {
        for _ in 0..BATCH {
            for _ in 0..10 {
                let shown = format!("[{}]", naive::fmt_stack(black_box(&STACK)));
                let painted = format!("\x1b[2m{shown}\x1b[0m");
                black_box(painted);
            }
        }
    });

    let fast = measure(|| {
        let mut buffer = String::new();
        for _ in 0..BATCH {
            for _ in 0..10 {
                buffer.clear();
                buffer.push_str("\x1b[2m[");
                write_stack(&mut buffer, black_box(&STACK));
                let _ = write!(buffer, "]\x1b[0m");
                black_box(&buffer);
            }
        }
    });

    report("invite complete (pile + couleur)", operations, naive, fast);
}

fn main() {
    if cfg!(debug_assertions) {
        eprintln!(
            "ATTENTION : compile en debug. Relance avec\n  \
             cargo run --release --example bench\n"
        );
    }

    println!(
        "{:<34} {:>12} {:>12}   gain",
        "banc d'essai", "naif", "optimise"
    );
    println!("{}", "-".repeat(76));

    bench_eval();
    bench_eval_errors();
    bench_trace();
    bench_format();
    bench_prompt();
}
