const express = require('express');
const router  = express.Router();
const db      = require('../db');
const MAP_TYPES = ['desert', 'snow', 'grassland', 'canyon', 'volcanic'];

// Generates a random 6-character room code like "AB12CD"
function generateRoomCode() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let code = '';
    for (let i = 0; i < 6; i++) {
        code += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return code;
}

// POST /games  — create a new game room
router.post('/', async (req, res) => {
    const { playerId, mapType } = req.body;
    if (!playerId) return res.status(400).json({ error: 'playerId required' });
    const selectedMapType = (mapType || 'desert').toLowerCase();
    if (!MAP_TYPES.includes(selectedMapType)) {
        return res.status(400).json({ error: 'Invalid map type' });
    }

    try {
        // Generate a unique room code
        let roomCode;
        let attempts = 0;
        while (attempts < 10) {
            roomCode = generateRoomCode();
            const [existing] = await db.query(
                'SELECT id FROM games WHERE room_code = ?', [roomCode]
            );
            if (existing.length === 0) break;
            attempts++;
        }

        const [result] = await db.query(
            `INSERT INTO games (room_code, player1_id, map_type, status)
             VALUES (?, ?, ?, 'waiting')`,
            [roomCode, playerId, selectedMapType]
        );

        res.json({ gameId: result.insertId, roomCode, mapType: selectedMapType });
    } catch (err) {
        console.error(err);
        res.status(500).json({ error: 'Could not create game' });
    }
});

// GET /games/room/:code  — find a game by room code
router.get('/room/:code', async (req, res) => {
    const code = req.params.code.toUpperCase();
    try {
        const [rows] = await db.query(
            'SELECT * FROM games WHERE room_code = ?', [code]
        );
        if (rows.length === 0) return res.status(404).json({ error: 'Room not found' });
        res.json(rows[0]);
    } catch (err) {
        console.error(err);
        res.status(500).json({ error: 'Server error' });
    }
});

// GET /games/:id  — get game status (used for polling in WaitingScene)
router.get('/:id', async (req, res) => {
    const id = parseInt(req.params.id);
    if (isNaN(id)) return res.status(400).json({ error: 'Invalid game id' });

    try {
        const [rows] = await db.query('SELECT * FROM games WHERE id = ?', [id]);
        if (rows.length === 0) return res.status(404).json({ error: 'Game not found' });
        res.json(rows[0]);
    } catch (err) {
        console.error(err);
        res.status(500).json({ error: 'Server error' });
    }
});

// POST /games/:id/join  — player 2 joins an existing room
router.post('/:id/join', async (req, res) => {
    const id       = parseInt(req.params.id);
    const { playerId } = req.body;

    if (isNaN(id))    return res.status(400).json({ error: 'Invalid game id' });
    if (!playerId)    return res.status(400).json({ error: 'playerId required' });

    try {
        const [rows] = await db.query('SELECT * FROM games WHERE id = ?', [id]);
        if (rows.length === 0) return res.status(404).json({ error: 'Game not found' });

        const game = rows[0];
        if (game.status !== 'waiting') {
            return res.status(400).json({ error: 'Game is no longer accepting players' });
        }
        if (game.player1_id === playerId) {
            return res.status(400).json({ error: 'You cannot join your own room' });
        }
        if (game.player2_id) {
            return res.status(400).json({ error: 'Room is full' });
        }

        await db.query(
            `UPDATE games SET player2_id = ?, status = 'in_progress' WHERE id = ?`,
            [playerId, id]
        );

        res.json({ message: 'Joined successfully', gameId: id });
    } catch (err) {
        console.error(err);
        res.status(500).json({ error: 'Server error' });
    }
});

module.exports = router;
