use std::fmt;

use crate::ops;

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

    pub(crate) fn stack_mut(&mut self) -> &mut Vec<f64> {
        &mut self.stack
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
            None => ops::apply(self, token),
        }
    }

    pub(crate) fn pop(&mut self, op: &str) -> Result<f64, EvalError> {
        match self.stack.pop() {
            Some(x) => Ok(x),
            None => Err(EvalError::NeedsOperands {
                op: op.to_string(),
                need: 1,
                got: 0,
            }),
        }
    }

    pub(crate) fn pop2(&mut self, op: &str) -> Result<(f64, f64), EvalError> {
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

impl std::error::Error for EvalError {}

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
