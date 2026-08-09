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

    fn paint(self, code: &str, text: &str) -> String {
        if self.color {
            format!("\x1b[{code}m{text}\x1b[0m")
        } else {
            text.to_string()
        }
    }

    pub fn bold(self, text: &str) -> String {
        self.paint("1", text)
    }

    pub fn dim(self, text: &str) -> String {
        self.paint("2", text)
    }

    pub fn red(self, text: &str) -> String {
        self.paint("31", text)
    }

    pub fn green(self, text: &str) -> String {
        self.paint("32", text)
    }

    pub fn yellow(self, text: &str) -> String {
        self.paint("33", text)
    }

    pub fn cyan(self, text: &str) -> String {
        self.paint("36", text)
    }
}
