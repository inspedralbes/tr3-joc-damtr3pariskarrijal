const GameRepository = require('./GameRepository');
const db = require('../db');

class GameRepositoryMySQL extends GameRepository {
  async create(roomCode, player1Id) {
    const [result] = await db.query(
      'INSERT INTO games (room_code, player1_id, status) VALUES (?, ?, "waiting")',
      [roomCode, player1Id]
    );
    return result.insertId;
  }
  async findByRoomCode(roomCode) {
    const [rows] = await db.query('SELECT * FROM games WHERE room_code = ?', [roomCode]);
    return rows[0] || null;
  }
  async findById(id) {
    const [rows] = await db.query('SELECT * FROM games WHERE id = ?', [id]);
    return rows[0] || null;
  }
  async joinGame(gameId, player2Id) {
    await db.query(
      'UPDATE games SET player2_id = ?, status = "in_progress" WHERE id = ?',
      [player2Id, gameId]
    );
  }
  async updateStatus(gameId, status) {
    await db.query('UPDATE games SET status = ? WHERE id = ?', [status, gameId]);
  }
}
module.exports = GameRepositoryMySQL;