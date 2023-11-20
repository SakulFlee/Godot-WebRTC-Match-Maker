use std::{error::Error, fmt::Display};

use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct AppConfig {
    pub listen_address: String,
    pub listen_port: u16,
}

impl AppConfig {
    pub fn load() -> Result<Self, Box<dyn Error>> {
        let app_config: AppConfig = confy::load("MatchMaker", None)?;

        println!(
            "Config path: {}
{}
",
            confy::get_configuration_file_path("MatchMaker", None)?
                .to_str()
                .unwrap(),
            app_config
        );

        Ok(app_config)
    }

    pub fn listen_string(&self) -> String {
        format!("{}:{}", self.listen_address, self.listen_port)
    }
}

impl Default for AppConfig {
    fn default() -> Self {
        Self {
            listen_address: String::from("0.0.0.0"),
            listen_port: 33333,
        }
    }
}

impl Display for AppConfig {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "AppConfig:
\tListen Address:\t{}
\tListen Port:\t{}",
            self.listen_address, self.listen_port
        )
    }
}
