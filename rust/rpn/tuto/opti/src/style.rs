use std::borrow::Cow;
use std::io::IsTerminal;

#[derive(Debug, Clone, Copy)]
pub struct Style {
    color: bool,
}

impl Style {
    pub fn detect() -> Style {
        let asked_no_color = std::env::var_os("NO_COLOR").is_some();
        let is_terminal = std::io::stdout().is_terminal();
        Style {
            color: !asked_no_color && is_terminal,
        }
    }

    #[inline]
    fn paint<'a>(self, code: &str, text: &'a str) -> Cow<'a, str> {
        if self.color {
            Cow::Owned(format!("\x1b[{code}m{text}\x1b[0m"))
        } else {
            Cow::Borrowed(text)
        }
    }

    pub fn bold(self, text: &str) -> Cow<'_, str> {
        self.paint("1", text)
    }

    pub fn dim(self, text: &str) -> Cow<'_, str> {
        self.paint("2", text)
    }

    pub fn red(self, text: &str) -> Cow<'_, str> {
        self.paint("31", text)
    }

    pub fn green(self, text: &str) -> Cow<'_, str> {
        self.paint("32", text)
    }

    pub fn yellow(self, text: &str) -> Cow<'_, str> {
        self.paint("33", text)
    }

    pub fn cyan(self, text: &str) -> Cow<'_, str> {
        self.paint("36", text)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn sans_couleur_le_texte_n_est_pas_realloue() {
        let style = Style { color: false };
        assert!(matches!(style.red("bonjour"), Cow::Borrowed("bonjour")));
    }

    #[test]
    fn avec_couleur_le_texte_est_encadre() {
        let style = Style { color: true };
        assert_eq!(style.red("bonjour"), "\x1b[31mbonjour\x1b[0m");
    }
}
