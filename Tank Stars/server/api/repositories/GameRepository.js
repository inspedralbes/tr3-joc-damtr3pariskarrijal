class GameRepository {
  async create(roomCode, player1Id) { throw new Error('Not implemented'); }
  async findByRoomCode(roomCode) { throw new Error('Not implemented'); }
  async findById(id) { throw new Error('Not implemented'); }
  async joinGame(gameId, player2Id) { throw new Error('Not implemented'); }
  async updateStatus(gameId, status) { throw new Error('Not implemented'); }
}
module.exports = GameRepository;