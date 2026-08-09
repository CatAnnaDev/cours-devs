use std::fmt;
use std::ops::Range;

use crate::ops;

#[derive(Debug, Default)]
pub struct Engine {
    stack: Vec<f64>,
    scratch: Vec<f64>,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum EvalError<'a> {
    NeedsOperands {
        op: &'a str,
        need: usize,
        got: usize,
    },
    Unknown(&'a str),
    DivByZero,
    Domain(&'a str),
}

#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Step<'a> {
    pub token: &'a str,
    pub before: &'a [f64],
    pub after: &'a [f64],
}

#[derive(Debug, Default)]
pub struct Trace {
    tokens: Vec<(u32, u32)>,
    offsets: Vec<u32>,
    values: Vec<f64>,
}

impl Trace {
    pub fn new() -> Self {
        Trace::default()
    }

    pub fn len(&self) -> usize {
        self.tokens.len()
    }

    pub fn is_empty(&self) -> bool {
        self.tokens.is_empty()
    }

    pub fn iter<'a>(&'a self, line: &'a str) -> impl Iterator<Item = Step<'a>> {
        self.tokens
            .iter()
            .enumerate()
            .map(|(i, &(start, end))| Step {
                token: &line[start as usize..end as usize],
                before: &self.values[self.span(i)],
                after: &self.values[self.span(i + 1)],
            })
    }

    fn span(&self, state: usize) -> Range<usize> {
        self.offsets[state] as usize..self.offsets[state + 1] as usize
    }

    fn reset(&mut self) {
        self.tokens.clear();
        self.offsets.clear();
        self.values.clear();
    }

    fn push_state(&mut self, state: &[f64]) {
        self.offsets.push(self.values.len() as u32);
        self.values.extend_from_slice(state);
    }

    fn seal(&mut self) {
        self.offsets.push(self.values.len() as u32);
    }
}

impl Engine {
    pub fn new() -> Self {
        Engine::default()
    }

    #[inline]
    pub fn stack(&self) -> &[f64] {
        &self.stack
    }

    #[inline]
    pub(crate) fn stack_mut(&mut self) -> &mut Vec<f64> {
        &mut self.stack
    }

    pub fn eval_line<'a>(&mut self, line: &'a str) -> Result<(), EvalError<'a>> {
        let mut sink = Trace::default();
        self.run::<false>(line, &mut sink)
    }

    pub fn eval_traced<'a>(
        &mut self,
        line: &'a str,
        trace: &mut Trace,
    ) -> Result<(), EvalError<'a>> {
        self.run::<true>(line, trace)
    }

    fn run<'a, const TRACE: bool>(
        &mut self,
        line: &'a str,
        trace: &mut Trace,
    ) -> Result<(), EvalError<'a>> {
        self.scratch.clear();
        self.scratch.extend_from_slice(&self.stack);

        if TRACE {
            trace.reset();
            trace.push_state(&self.stack);
        }

        let base = line.as_ptr() as usize;

        for token in line.split_whitespace() {
            if let Err(error) = self.eval_token(token) {
                std::mem::swap(&mut self.stack, &mut self.scratch);
                if TRACE {
                    trace.reset();
                }
                return Err(error);
            }
            if TRACE {
                let start = (token.as_ptr() as usize - base) as u32;
                trace.tokens.push((start, start + token.len() as u32));
                trace.push_state(&self.stack);
            }
        }

        if TRACE {
            trace.seal();
        }
        Ok(())
    }

    #[inline]
    fn eval_token<'a>(&mut self, token: &'a str) -> Result<(), EvalError<'a>> {
        match parse_number(token) {
            Some(number) => {
                self.stack.push(number);
                Ok(())
            }
            None => ops::apply(self, token),
        }
    }

    #[inline]
    pub(crate) fn pop<'a>(&mut self, op: &'a str) -> Result<f64, EvalError<'a>> {
        self.stack.pop().ok_or(EvalError::NeedsOperands {
            op,
            need: 1,
            got: 0,
        })
    }

    #[inline]
    pub(crate) fn pop2<'a>(&mut self, op: &'a str) -> Result<(f64, f64), EvalError<'a>> {
        let len = self.stack.len();
        if len < 2 {
            return Err(EvalError::NeedsOperands {
                op,
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

#[inline]
fn parse_number(token: &str) -> Option<f64> {
    match token.parse::<f64>() {
        Ok(number) if number.is_finite() => Some(number),
        _ => None,
    }
}

impl fmt::Display for EvalError<'_> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
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

impl std::error::Error for EvalError<'_> {}

#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Num(pub f64);

impl fmt::Display for Num {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        let n = self.0;
        if n == 0.0 {
            return f.write_str("0");
        }
        if n.fract() == 0.0 && n.abs() < 1e15 {
            return write!(f, "{}", n as i64);
        }
        write!(f, "{n}")
    }
}

pub fn write_stack(out: &mut String, stack: &[f64]) {
    use fmt::Write;

    for (index, &n) in stack.iter().enumerate() {
        if index > 0 {
            out.push(' ');
        }
        let _ = write!(out, "{}", Num(n));
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn eval(line: &str) -> Vec<f64> {
        let mut engine = Engine::new();
        engine.eval_line(line).expect("évaluation valide");
        engine.stack().to_vec()
    }

    #[test]
    fn empile_les_nombres() {
        assert_eq!(eval("1 2 3"), [1.0, 2.0, 3.0]);
    }

    #[test]
    fn respecte_l_ordre_des_operandes() {
        assert_eq!(eval("10 3 -"), [7.0]);
        assert_eq!(eval("10 4 /"), [2.5]);
    }

    #[test]
    fn enchaine_les_operations() {
        assert_eq!(eval("5 1 2 + 4 * + 3 -"), [14.0]);
    }

    #[test]
    fn accepte_decimaux_negatifs_et_scientifique() {
        assert_eq!(eval("-1.5e3"), [-1500.0]);
    }

    #[test]
    fn refuse_les_valeurs_non_finies() {
        let mut engine = Engine::new();
        assert_eq!(engine.eval_line("inf"), Err(EvalError::Unknown("inf")));
        assert_eq!(engine.eval_line("NaN"), Err(EvalError::Unknown("NaN")));
    }

    #[test]
    fn signale_le_manque_d_operandes() {
        let mut engine = Engine::new();
        assert_eq!(
            engine.eval_line("3 +"),
            Err(EvalError::NeedsOperands {
                op: "+",
                need: 2,
                got: 1
            })
        );
    }

    #[test]
    fn restaure_la_pile_apres_une_erreur() {
        let mut engine = Engine::new();
        engine.eval_line("1 2 3").unwrap();
        assert!(engine.eval_line("+ oups *").is_err());
        assert_eq!(engine.stack(), [1.0, 2.0, 3.0]);
    }

    #[test]
    fn conserve_la_pile_entre_deux_lignes() {
        let mut engine = Engine::new();
        engine.eval_line("3 4").unwrap();
        engine.eval_line("+").unwrap();
        assert_eq!(engine.stack(), [7.0]);
    }

    #[test]
    fn trace_chaque_jeton() {
        let mut engine = Engine::new();
        let mut trace = Trace::new();
        let line = "3 4 +";
        engine.eval_traced(line, &mut trace).unwrap();

        let steps: Vec<Step<'_>> = trace.iter(line).collect();
        assert_eq!(steps.len(), 3);
        assert_eq!(steps[0].token, "3");
        assert_eq!(steps[0].before, [] as [f64; 0]);
        assert_eq!(steps[0].after, [3.0]);
        assert_eq!(steps[2].token, "+");
        assert_eq!(steps[2].before, [3.0, 4.0]);
        assert_eq!(steps[2].after, [7.0]);
    }

    #[test]
    fn la_trace_repart_propre_apres_une_erreur() {
        let mut engine = Engine::new();
        let mut trace = Trace::new();
        assert!(engine.eval_traced("1 2 oups", &mut trace).is_err());
        assert!(trace.is_empty());

        engine.eval_traced("3 4 +", &mut trace).unwrap();
        assert_eq!(trace.len(), 3);
    }

    #[test]
    fn la_trace_est_reutilisable_sans_realloc() {
        let mut engine = Engine::new();
        let mut trace = Trace::new();
        engine.eval_traced("1 2 3 4 5 + + + +", &mut trace).unwrap();
        let capacity = trace.values.capacity();

        engine.eval_traced("1 1 +", &mut trace).unwrap();
        assert_eq!(trace.values.capacity(), capacity);
    }

    #[test]
    fn les_jetons_de_la_trace_pointent_au_bon_endroit() {
        let mut engine = Engine::new();
        let mut trace = Trace::new();
        let line = "  12   dup   *  ";
        engine.eval_traced(line, &mut trace).unwrap();

        let tokens: Vec<&str> = trace.iter(line).map(|step| step.token).collect();
        assert_eq!(tokens, ["12", "dup", "*"]);
    }

    #[test]
    fn affiche_les_entiers_sans_partie_decimale() {
        assert_eq!(Num(7.0).to_string(), "7");
        assert_eq!(Num(-0.0).to_string(), "0");
        assert_eq!(Num(2.5).to_string(), "2.5");

        let mut buffer = String::new();
        write_stack(&mut buffer, &[1.0, 2.5]);
        assert_eq!(buffer, "1 2.5");
    }
}
