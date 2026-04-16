const mysql = require('mysql2/promise');

const pool = mysql.createPool({
  host: process.env.DB_HOST || 'localhost',
  user: process.env.DB_USER || 'root',
  password: process.env.DB_PASSWORD || 'root',
  database: process.env.DB_NAME || 'tankstars',
});

async function ensureSchema() {
  const [rows] = await pool.query("SHOW COLUMNS FROM games LIKE 'map_type'");
  if (rows.length === 0) {
    await pool.query(`
      ALTER TABLE games
      ADD COLUMN map_type VARCHAR(20) NOT NULL DEFAULT 'desert'
    `);
  }
}

module.exports = pool;
module.exports.ensureSchema = ensureSchema;
