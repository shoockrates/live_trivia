import React, { useState, useEffect } from 'react';
import './Leaderboard.css';

const Leaderboard = ({ onBack }) => {
  const [leaderboard, setLeaderboard] = useState([]);
  const [loading, setLoading] = useState(true);
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [categories, setCategories] = useState([]);
  const [error, setError] = useState('');

  const API_BASE = 'http://localhost:5216';

  useEffect(() => {
    fetchLeaderboard();
    fetchCategories();
  }, [categoryFilter]);

  const fetchLeaderboard = async () => {
    setLoading(true);
    setError('');
    try {
      let url = `${API_BASE}/leaderboard/top?top=10`;
      
      if (categoryFilter !== 'all') {
        url = `${API_BASE}/leaderboard/category/${encodeURIComponent(categoryFilter)}?top=10`;
      }

      console.log('Fetching leaderboard from:', url);
      
      const response = await fetch(url);
      if (response.ok) {
        const data = await response.json();
        console.log('Leaderboard data received:', data);
        setLeaderboard(data);
      } else {
        const errorText = await response.text();
        setError(`Failed to load leaderboard: ${errorText}`);
        console.error('Failed to fetch leaderboard:', response.status, errorText);
      }
    } catch (error) {
      setError('Error connecting to server. Make sure the backend is running.');
      console.error('Error fetching leaderboard:', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchCategories = async () => {
    try {
      const response = await fetch(`${API_BASE}/leaderboard/categories`);
      if (response.ok) {
        const data = await response.json();
        setCategories(data);
        console.log('Categories loaded:', data);
      } else {
        console.error('Failed to fetch categories');
      }
    } catch (error) {
      console.error('Error fetching categories:', error);
    }
  };

  const getRankIcon = (rank) => {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return `#${rank}`;
  };

  const getRankClass = (rank) => {
    if (rank <= 3) return 'top-three';
    if (rank <= 10) return 'top-ten';
    return 'other';
  };

  const formatLastPlayed = (lastPlayedAt) => {
    if (!lastPlayedAt) return 'Never';
    const date = new Date(lastPlayedAt);
    const now = new Date();
    const diffDays = Math.floor((now - date) / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    return date.toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="leaderboard-container">
        <div className="leaderboard-card">
          <div className="loading-spinner"></div>
          <p>Loading leaderboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="leaderboard-container">
      <div className="leaderboard-card">
        <div className="leaderboard-header">
          <h2 className="leaderboard-title">Leaderboard</h2>
          <p className="leaderboard-subtitle">Top trivia players</p>
          
          {error && (
            <div className="error-message">
              {error}
            </div>
          )}

          <div className="filter-section">
            <div className="filter-group">
              <label>Filter by Category:</label>
              <select 
                value={categoryFilter} 
                onChange={(e) => setCategoryFilter(e.target.value)}
                className="filter-select"
              >
                <option value="all">All Categories</option>
                {categories.map(category => (
                  <option key={category} value={category}>{category}</option>
                ))}
              </select>
            </div>
          </div>
        </div>

        <div className="leaderboard-list">
          {leaderboard.length === 0 ? (
            <div className="no-data">
              <svg width="48" height="48" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
              </svg>
              <h3>No Players Yet</h3>
              <p>Be the first to play and get on the leaderboard!</p>
            </div>
          ) : (
            leaderboard.map((player) => (
              <div key={player.playerId || player.username} className={`leaderboard-item ${getRankClass(player.rank)}`}>
                <div className="rank-section">
                  <div className="rank-icon">{getRankIcon(player.rank)}</div>
                  <div className="rank-number">{player.rank}</div>
                </div>
                
                <div className="player-info">
                  <div className="player-avatar">
                    {player.username?.charAt(0)?.toUpperCase() || 'P'}
                  </div>
                  <div className="player-details">
                    <div className="player-name">{player.username}</div>
                    <div className="player-stats">
                      {player.gamesPlayed} game{player.gamesPlayed !== 1 ? 's' : ''} • {player.accuracy}% accuracy
                      {player.category && ` • ${player.category}`}
                    </div>
                    <div className="last-played">
                      Last played: {formatLastPlayed(player.lastPlayedAt)}
                    </div>
                  </div>
                </div>
                
                <div className="score-section">
                  <div className="score-value">{player.totalScore?.toLocaleString() || 0}</div>
                  <div className="score-label">total points</div>
                  <div className="best-score">Best: {player.bestScore}%</div>
                </div>
              </div>
            ))
          )}
        </div>

        <div className="leaderboard-footer">
          <button className="refresh-button" onClick={fetchLeaderboard}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
              <path d="M17.65 6.35C16.2 4.9 14.21 4 12 4 7.58 4 4.01 7.58 4.01 12s3.57 8 8 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"/>
            </svg> 
          </button>
          
          <button className="back-button" onClick={onBack}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M8 0L0 8l8 8V0z"/>
            </svg>
            Back
          </button>
        </div>
      </div>
    </div>
  );
};

export default Leaderboard;
