use crate::eval::{Engine, EvalError};

pub fn apply<'a>(engine: &mut Engine, token: &'a str) -> Result<(), EvalError<'a>> {
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
            stack.reserve(2);
            stack.push(x);
            stack.push(x);
            Ok(())
        }
        "drop" => engine.pop(token).map(|_| ()),
        "swap" => {
            let len = engine.stack().len();
            if len < 2 {
                return Err(EvalError::NeedsOperands {
                    op: token,
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
            stack.reserve(3);
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
                    op: token,
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

        other => Err(EvalError::Unknown(other)),
    }
}

#[inline]
fn push(engine: &mut Engine, n: f64) -> Result<(), EvalError<'static>> {
    engine.stack_mut().push(n);
    Ok(())
}

#[inline]
fn binary<'a>(
    engine: &mut Engine,
    op: &'a str,
    f: impl FnOnce(f64, f64) -> Result<f64, EvalError<'a>>,
) -> Result<(), EvalError<'a>> {
    let (a, b) = engine.pop2(op)?;
    let result = f(a, b)?;
    engine.stack_mut().push(result);
    Ok(())
}

#[inline]
fn unary<'a>(
    engine: &mut Engine,
    op: &'a str,
    f: impl FnOnce(f64) -> Result<f64, EvalError<'a>>,
) -> Result<(), EvalError<'a>> {
    let a = engine.pop(op)?;
    let result = f(a)?;
    engine.stack_mut().push(result);
    Ok(())
}

fn reduce<'a>(
    engine: &mut Engine,
    op: &'a str,
    init: f64,
    f: impl Fn(f64, f64) -> f64,
) -> Result<(), EvalError<'a>> {
    let stack = engine.stack_mut();
    if stack.is_empty() {
        return Err(EvalError::NeedsOperands {
            op,
            need: 1,
            got: 0,
        });
    }
    let total = stack.drain(..).fold(init, f);
    stack.push(total);
    Ok(())
}

#[inline]
fn nonzero<'a>(x: f64) -> Result<f64, EvalError<'a>> {
    if x == 0.0 {
        Err(EvalError::DivByZero)
    } else {
        Ok(x)
    }
}

#[inline]
fn domain<'a>(op: &'a str, ok: bool, value: f64) -> Result<f64, EvalError<'a>> {
    if ok {
        Ok(value)
    } else {
        Err(EvalError::Domain(op))
    }
}

fn factorial<'a>(op: &'a str, x: f64) -> Result<f64, EvalError<'a>> {
    if x < 0.0 || x.fract() != 0.0 || x > 170.0 {
        return Err(EvalError::Domain(op));
    }
    Ok((1..=x as u64).map(|i| i as f64).product())
}

#[cfg(test)]
mod tests {
    use crate::eval::{Engine, EvalError};

    fn eval(line: &str) -> Vec<f64> {
        let mut engine = Engine::new();
        engine.eval_line(line).expect("évaluation valide");
        engine.stack().to_vec()
    }

    fn error(line: &str) -> EvalError<'_> {
        let mut engine = Engine::new();
        engine.eval_line(line).expect_err("erreur attendue")
    }

    #[test]
    fn arithmetique() {
        assert_eq!(eval("2 3 ^"), [8.0]);
        assert_eq!(eval("7 3 %"), [1.0]);
        assert_eq!(eval("-7 3 %"), [2.0]);
        assert_eq!(eval("3 9 min"), [3.0]);
        assert_eq!(eval("3 9 max"), [9.0]);
    }

    #[test]
    fn fonctions_unaires() {
        assert_eq!(eval("9 sqrt"), [3.0]);
        assert_eq!(eval("1 exp ln"), [1.0]);
        assert_eq!(eval("5 fact"), [120.0]);
        assert_eq!(eval("0 !"), [1.0]);
        assert_eq!(eval("-4 abs"), [4.0]);
        assert_eq!(eval("2.7 floor"), [2.0]);
    }

    #[test]
    fn manipulation_de_pile() {
        assert_eq!(eval("3 dup"), [3.0, 3.0]);
        assert_eq!(eval("1 2 drop"), [1.0]);
        assert_eq!(eval("1 2 swap"), [2.0, 1.0]);
        assert_eq!(eval("1 2 over"), [1.0, 2.0, 1.0]);
        assert_eq!(eval("1 2 3 rot"), [2.0, 3.0, 1.0]);
        assert_eq!(eval("1 2 3 clear"), []);
    }

    #[test]
    fn reductions() {
        assert_eq!(eval("1 2 3 4 sum"), [10.0]);
        assert_eq!(eval("1 2 3 4 prod"), [24.0]);
    }

    #[test]
    fn garde_fous() {
        assert_eq!(error("1 0 /"), EvalError::DivByZero);
        assert_eq!(error("1 0 %"), EvalError::DivByZero);
        assert_eq!(error("0 inv"), EvalError::DivByZero);
        assert_eq!(error("-1 sqrt"), EvalError::Domain("sqrt"));
        assert_eq!(error("0 ln"), EvalError::Domain("ln"));
        assert_eq!(error("171 fact"), EvalError::Domain("fact"));
        assert_eq!(error("2.5 fact"), EvalError::Domain("fact"));
        assert_eq!(error("bidule"), EvalError::Unknown("bidule"));
    }
}
