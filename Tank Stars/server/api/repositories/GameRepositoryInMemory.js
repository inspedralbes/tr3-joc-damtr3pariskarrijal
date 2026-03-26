const GameRepository = require('./GameRepository');

class GameRepositoryInMemory extends GameRepository {
  constructor() {
    super();
    this.games = [];
    this.nextId = 1;
  }
  async create(roomCode, player1Id) {
    const game = { id: this.nextId++, room_code: roomCode, player1_id: player1Id, player2_id: null, status: 'waiting' };
    this.games.push(game);
    return game.id;
  }
  async findByRoomCode(roomCode) {
    return this.games.find(g => g.room_code === roomCode) || null;
  }
  async findById(id) {
    return this.games.find(g => g.id === id) || null;
  }
  async joinGame(gameId, player2Id) {
    const game = this.games.find(g => g.id === gameId);
    if (game) { game.player2_id = player2Id; game.status = 'in_progress'; }
  }
  async updateStatus(gameId, status) {
    const game = this.games.find(g => g.id === gameId);
    if (game) game.status = status;
  }
}
module.exports = GameRepositoryInMemory;