CREATE DATABASE IF NOT EXISTS tankstars;
USE tankstars;

CREATE TABLE users (
  id INT AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) NOT NULL UNIQUE,
  password VARCHAR(255) NOT NULL,
  wins INT DEFAULT 0,
  losses INT DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE games (
  id INT AUTO_INCREMENT PRIMARY KEY,
  room_code VARCHAR(10) NOT NULL UNIQUE,
  player1_id INT,
  player2_id INT,
  map_type VARCHAR(20) NOT NULL DEFAULT 'desert',
  status ENUM('waiting', 'in_progress', 'finished') DEFAULT 'waiting',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (player1_id) REFERENCES users(id),
  FOREIGN KEY (player2_id) REFERENCES users(id)
);

CREATE TABLE results (
  id INT AUTO_INCREMENT PRIMARY KEY,
  game_id INT,
  winner_id INT,
  loser_id INT,
  winner_hp INT,
  duration_seconds INT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (game_id) REFERENCES games(id),
  FOREIGN KEY (winner_id) REFERENCES users(id),
  FOREIGN KEY (loser_id) REFERENCES users(id)
);
