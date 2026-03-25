const express = require('express');
const cors = require('cors');
const app = express();

app.use(cors());
app.use(express.json());

const authRoutes = require('./routes/auth');
app.use('/auth', authRoutes);

app.get('/health', (req, res) => {
  res.json({ status: 'API running' });
});

app.listen(3001, () => {
  console.log('API Service running on port 3001');
});