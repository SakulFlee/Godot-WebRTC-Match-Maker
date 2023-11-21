use std::{collections::HashMap, error::Error, fmt::Display};

use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct AppConfig {
    pub listen_address: String,
    pub listen_port: u16,
    pub slots: HashMap<String, u8>,
}

impl AppConfig {
    pub fn load() -> Result<Self, Box<dyn Error>> {
        println!(
            "Config path: {}",
            confy::get_configuration_file_path("MatchMaker", None)?
                .to_str()
                .unwrap()
        );

        let app_config: AppConfig = confy::load("MatchMaker", None)?;

        println!("{}", app_config);

        Ok(app_config)
    }

    pub fn listen_string(&self) -> String {
        format!("{}:{}", self.listen_address, self.listen_port)
    }
}

impl Default for AppConfig {
    fn default() -> Self {
        let mut slots = HashMap::new();
        slots.insert(String::from("Test"), 2);

        Self {
            listen_address: String::from("0.0.0.0"),
            listen_port: 33333,
            slots,
        }
    }
}

impl Display for AppConfig {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "AppConfig:
\tListen Address:\t{}
\tListen Port:\t{}
\tQueue Slot Config:
{}",
            self.listen_address,
            self.listen_port,
            self.slots
                .iter()
                .map(|(k, v)| format!("\t\t\"{}\": {} slots", k, v))
                .collect::<Vec<String>>()
                .join("\n")
        )
    }
}
