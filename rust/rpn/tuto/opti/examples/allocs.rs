use std::alloc::{GlobalAlloc, Layout, System};
use std::hint::black_box;
use std::sync::atomic::{AtomicUsize, Ordering};

use blap_opti::eval::{Engine, Trace, write_stack};
use blap_opti::naive;

static ALLOCATIONS: AtomicUsize = AtomicUsize::new(0);

struct Counting;

unsafe impl GlobalAlloc for Counting {
    unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
        ALLOCATIONS.fetch_add(1, Ordering::Relaxed);
        unsafe { System.alloc(layout) }
    }

    unsafe fn dealloc(&self, ptr: *mut u8, layout: Layout) {
        unsafe { System.dealloc(ptr, layout) }
    }

    unsafe fn realloc(&self, ptr: *mut u8, layout: Layout, new_size: usize) -> *mut u8 {
        ALLOCATIONS.fetch_add(1, Ordering::Relaxed);
        unsafe { System.realloc(ptr, layout, new_size) }
    }
}

#[global_allocator]
static ALLOCATOR: Counting = Counting;

const LINES: [&str; 6] = [
    "5 1 2 + 4 * + 3 -",
    "3 dup * 4 dup * + sqrt",
    "1 2 3 4 5 sum",
    "2 10 pow 2 log2",
    "pi 2 / sin 1 +",
    "10 3 % 7 max 2 min",
];

const STACK: [f64; 8] = [1.0, -2.5, 3.0, 1e-3, 42.0, -0.0, 1234.5, 7.0];

fn count(mut body: impl FnMut()) -> usize {
    body();
    body();
    let before = ALLOCATIONS.load(Ordering::Relaxed);
    body();
    ALLOCATIONS.load(Ordering::Relaxed) - before
}

fn report(label: &str, naive: usize, fast: usize) {
    println!("{label:<34} {naive:>10} {fast:>10}");
}

fn main() {
    println!("allocations par passe sur {} lignes", LINES.len());
    println!("{:<34} {:>10} {:>10}", "", "naif", "optimise");
    println!("{}", "-".repeat(56));

    let naive_eval = {
        let mut engine = naive::Engine::new();
        count(|| {
            for line in LINES {
                engine.eval_line(black_box(line)).unwrap();
            }
            engine.eval_line("clear").unwrap();
        })
    };
    let fast_eval = {
        let mut engine = Engine::new();
        count(|| {
            for line in LINES {
                engine.eval_line(black_box(line)).unwrap();
            }
            engine.eval_line("clear").unwrap();
        })
    };
    report("evaluation", naive_eval, fast_eval);

    let naive_errors = {
        let mut engine = naive::Engine::new();
        count(|| {
            engine.eval_line(black_box("1 2 3 oups +")).ok();
            engine.eval_line(black_box("1 2 + +")).ok();
        })
    };
    let fast_errors = {
        let mut engine = Engine::new();
        count(|| {
            engine.eval_line(black_box("1 2 3 oups +")).ok();
            engine.eval_line(black_box("1 2 + +")).ok();
        })
    };
    report("2 lignes en erreur", naive_errors, fast_errors);

    let naive_trace = {
        let mut engine = naive::Engine::new();
        let mut rendered = String::new();
        count(|| {
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
        })
    };
    let fast_trace = {
        let mut engine = Engine::new();
        let mut trace = Trace::new();
        let mut rendered = String::new();
        count(|| {
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
        })
    };
    report("trace + rendu", naive_trace, fast_trace);

    let naive_format = count(|| {
        for _ in 0..10 {
            black_box(naive::fmt_stack(black_box(&STACK)));
        }
    });
    let fast_format = {
        let mut buffer = String::new();
        count(|| {
            for _ in 0..10 {
                buffer.clear();
                write_stack(&mut buffer, black_box(&STACK));
                black_box(&buffer);
            }
        })
    };
    report("10 formatages d'une pile de 8", naive_format, fast_format);
}
