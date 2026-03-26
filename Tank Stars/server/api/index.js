const express = require('express');
const cors    = require('cors');
const app     = express();

app.use(cors());
app.use(express.json());

// Routes
const authRoutes  = require('./routes/auth');
const gameRoutes  = require('./routes/games');

app.use('/auth',  authRoutes);
app.use('/games', gameRoutes);

// Health check
app.get('/health', (req, res) => {
    res.json({ status: 'API running' });
});

app.listen(3001, () => {
    console.log('API Service running on port 3001');
});