use std::fmt;

#[derive(Debug, Default)]
pub struct Engine {
    stack: Vec<f64>,
}

#[derive(Debug, Clone, PartialEq)]
pub struct Step {
    pub token: String,
    pub before: Vec<f64>,
    pub after: Vec<f64>,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum EvalError {
    NeedsOperands { op: String, need: usize, got: usize },
    Unknown(String),
    DivByZero,
    Domain(String),
}

impl Engine {
    pub fn new() -> Self {
        Engine { stack: Vec::new() }
    }

    pub fn stack(&self) -> &[f64] {
        &self.stack
    }

    pub fn eval_line(&mut self, line: &str) -> Result<(), EvalError> {
        self.run(line, false)?;
        Ok(())
    }

    pub fn eval_traced(&mut self, line: &str) -> Result<Vec<Step>, EvalError> {
        self.run(line, true)
    }

    fn run(&mut self, line: &str, trace: bool) -> Result<Vec<Step>, EvalError> {
        let backup = self.stack.clone();
        let mut steps = Vec::new();

        for token in line.split_whitespace() {
            let before = if trace {
                self.stack.clone()
            } else {
                Vec::new()
            };

            if let Err(error) = self.eval_token(token) {
                self.stack = backup;
                return Err(error);
            }

            if trace {
                steps.push(Step {
                    token: token.to_string(),
                    before,
                    after: self.stack.clone(),
                });
            }
        }

        Ok(steps)
    }

    fn eval_token(&mut self, token: &str) -> Result<(), EvalError> {
        match parse_number(token) {
            Some(number) => {
                self.stack.push(number);
                Ok(())
            }
            None => apply(self, token),
        }
    }

    fn stack_mut(&mut self) -> &mut Vec<f64> {
        &mut self.stack
    }

    fn pop(&mut self, op: &str) -> Result<f64, EvalError> {
        match self.stack.pop() {
            Some(x) => Ok(x),
            None => Err(EvalError::NeedsOperands {
                op: op.to_string(),
                need: 1,
                got: 0,
            }),
        }
    }

    fn pop2(&mut self, op: &str) -> Result<(f64, f64), EvalError> {
        let len = self.stack.len();
        if len < 2 {
            return Err(EvalError::NeedsOperands {
                op: op.to_string(),
                need: 2,
                got: len,
            });
        }
        let a = self.stack[len - 2];
        let b = self.stack[len - 1];
        self.stack.truncate(len - 2);
        Ok((a, b))
    }
}

fn parse_number(token: &str) -> Option<f64> {
    match token.parse::<f64>() {
        Ok(number) if number.is_finite() => Some(number),
        _ => None,
    }
}

impl fmt::Display for EvalError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match self {
            EvalError::NeedsOperands { op, need, got } => {
                let s = if *need > 1 { "s" } else { "" };
                write!(f, "`{op}` attend {need} opérande{s} mais n'en a que {got}")
            }
            EvalError::Unknown(token) => write!(f, "jeton inconnu : `{token}`"),
            EvalError::DivByZero => write!(f, "division par zéro"),
            EvalError::Domain(op) => write!(f, "`{op}` : opération hors domaine"),
        }
    }
}

pub fn fmt_num(n: f64) -> String {
    if n == 0.0 {
        return String::from("0");
    }
    if n.fract() == 0.0 && n.abs() < 1e15 {
        return format!("{}", n as i64);
    }
    format!("{n}")
}

pub fn fmt_stack(stack: &[f64]) -> String {
    let parts: Vec<String> = stack.iter().map(|&n| fmt_num(n)).collect();
    parts.join(" ")
}

fn apply(engine: &mut Engine, token: &str) -> Result<(), EvalError> {
    match token {
        "pi" => push(engine, std::f64::consts::PI),
        "e" => push(engine, std::f64::consts::E),
        "tau" => push(engine, std::f64::consts::TAU),

        "+" => binary(engine, token, |a, b| Ok(a + b)),
        "-" => binary(engine, token, |a, b| Ok(a - b)),
        "*" => binary(engine, token, |a, b| Ok(a * b)),
        "/" => binary(engine, token, |a, b| nonzero(b).map(|b| a / b)),
        "%" | "mod" => binary(engine, token, |a, b| nonzero(b).map(|b| a.rem_euclid(b))),
        "^" | "**" | "pow" => binary(engine, token, |a, b| Ok(a.powf(b))),
        "min" => binary(engine, token, |a, b| Ok(a.min(b))),
        "max" => binary(engine, token, |a, b| Ok(a.max(b))),

        "neg" => unary(engine, token, |a| Ok(-a)),
        "abs" => unary(engine, token, |a| Ok(a.abs())),
        "inv" => unary(engine, token, |a| nonzero(a).map(|a| 1.0 / a)),
        "sqrt" => unary(engine, token, |a| domain(token, a >= 0.0, a.sqrt())),
        "exp" => unary(engine, token, |a| Ok(a.exp())),
        "ln" => unary(engine, token, |a| domain(token, a > 0.0, a.ln())),
        "log" | "log10" => unary(engine, token, |a| domain(token, a > 0.0, a.log10())),
        "log2" => unary(engine, token, |a| domain(token, a > 0.0, a.log2())),
        "sin" => unary(engine, token, |a| Ok(a.sin())),
        "cos" => unary(engine, token, |a| Ok(a.cos())),
        "tan" => unary(engine, token, |a| Ok(a.tan())),
        "floor" => unary(engine, token, |a| Ok(a.floor())),
        "ceil" => unary(engine, token, |a| Ok(a.ceil())),
        "round" => unary(engine, token, |a| Ok(a.round())),
        "!" | "fact" => unary(engine, token, |a| factorial(token, a)),

        "dup" => {
            let x = engine.pop(token)?;
            let stack = engine.stack_mut();
            stack.push(x);
            stack.push(x);
            Ok(())
        }
        "drop" => {
            engine.pop(token)?;
            Ok(())
        }
        "swap" => {
            let len = engine.stack().len();
            if len < 2 {
                return Err(EvalError::NeedsOperands {
                    op: token.to_string(),
                    need: 2,
                    got: len,
                });
            }
            engine.stack_mut().swap(len - 1, len - 2);
            Ok(())
        }
        "over" => {
            let (a, b) = engine.pop2(token)?;
            let stack = engine.stack_mut();
            stack.push(a);
            stack.push(b);
            stack.push(a);
            Ok(())
        }
        "rot" => {
            let stack = engine.stack_mut();
            let len = stack.len();
            if len < 3 {
                return Err(EvalError::NeedsOperands {
                    op: token.to_string(),
                    need: 3,
                    got: len,
                });
            }
            stack[len - 3..].rotate_left(1);
            Ok(())
        }
        "clear" | "cls" => {
            engine.stack_mut().clear();
            Ok(())
        }

        "sum" => reduce(engine, token, 0.0, |acc, x| acc + x),
        "prod" => reduce(engine, token, 1.0, |acc, x| acc * x),

        other => Err(EvalError::Unknown(other.to_string())),
    }
}

fn push(engine: &mut Engine, n: f64) -> Result<(), EvalError> {
    engine.stack_mut().push(n);
    Ok(())
}

fn binary(
    engine: &mut Engine,
    op: &str,
    f: impl Fn(f64, f64) -> Result<f64, EvalError>,
) -> Result<(), EvalError> {
    let (a, b) = engine.pop2(op)?;
    let result = f(a, b)?;
    engine.stack_mut().push(result);
    Ok(())
}

fn unary(
    engine: &mut Engine,
    op: &str,
    f: impl Fn(f64) -> Result<f64, EvalError>,
) -> Result<(), EvalError> {
    let a = engine.pop(op)?;
    let result = f(a)?;
    engine.stack_mut().push(result);
    Ok(())
}

fn reduce(
    engine: &mut Engine,
    op: &str,
    init: f64,
    f: impl Fn(f64, f64) -> f64,
) -> Result<(), EvalError> {
    let stack = engine.stack_mut();
    if stack.is_empty() {
        return Err(EvalError::NeedsOperands {
            op: op.to_string(),
            need: 1,
            got: 0,
        });
    }
    let total = stack.iter().fold(init, |acc, &x| f(acc, x));
    stack.clear();
    stack.push(total);
    Ok(())
}

fn nonzero(x: f64) -> Result<f64, EvalError> {
    if x == 0.0 {
        Err(EvalError::DivByZero)
    } else {
        Ok(x)
    }
}

fn domain(op: &str, ok: bool, value: f64) -> Result<f64, EvalError> {
    if ok {
        Ok(value)
    } else {
        Err(EvalError::Domain(op.to_string()))
    }
}

fn factorial(op: &str, x: f64) -> Result<f64, EvalError> {
    if x < 0.0 || x.fract() != 0.0 || x > 170.0 {
        return Err(EvalError::Domain(op.to_string()));
    }
    Ok((1..=x as u64).map(|i| i as f64).product())
}
