import React, { useState, useEffect } from 'react';
import './Profile.css';

const Profile = ({ user, onBack }) => {
  const [profileData, setProfileData] = useState({
    username: user?.username || '',
    playerId: user?.playerId || '',
    joinDate: '',
    totalGames: 0,
    recentGames: [],
    personalRecords: {
      highestScore: 0,
      bestAccuracy: 0,
      longestStreak: 0,
      fastestGame: null
    }
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchProfileData = async () => {
      try {
        setLoading(true);
        
        // Fetch player statistics
        const statsResponse = await fetch('http://localhost:5216/statistics/player', {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        });

        if (statsResponse.ok) {
          const stats = await statsResponse.json();
          
          // For now, we'll create mock recent games and records
          // In a real app, you'd fetch this from your backend
          const mockRecentGames = [
            { id: 1, category: 'Science', score: 85, accuracy: 85, date: '2024-01-15', duration: '2:30' },
            { id: 2, category: 'History', score: 92, accuracy: 92, date: '2024-01-14', duration: '3:15' },
            { id: 3, category: 'Geography', score: 78, accuracy: 78, date: '2024-01-13', duration: '2:45' },
            { id: 4, category: 'Science', score: 95, accuracy: 95, date: '2024-01-12', duration: '2:10' },
            { id: 5, category: 'Movies', score: 88, accuracy: 88, date: '2024-01-11', duration: '3:05' }
          ];

          const personalRecords = {
            highestScore: stats.bestScore || 0,
            bestAccuracy: stats.totalQuestionsAnswered > 0 ? 
              Math.round((stats.totalCorrectAnswers / stats.totalQuestionsAnswered) * 100) : 0,
            longestStreak: Math.floor(Math.random() * 10) + 1, // Mock data
            fastestGame: '1:45' // Mock data
          };

          setProfileData({
            username: user?.username || '',
            playerId: user?.playerId || '',
            joinDate: '2024-01-01', // Mock join date
            totalGames: stats.totalGamesPlayed || 0,
            recentGames: mockRecentGames,
            personalRecords
          });
        }
      } catch (error) {
        console.error('Error fetching profile data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchProfileData();
  }, [user]);

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  if (loading) {
    return (
      <div className="profile-container">
        <div className="profile-card">
          <div className="loading-spinner"></div>
          <p>Loading profile...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="profile-container">
      <div className="profile-card">
        {/* User Identity Section */}
        <div className="profile-header">
          <div className="user-identity">
            <div className="user-avatar-large">
              {profileData.username?.charAt(0)?.toUpperCase() || 'U'}
            </div>
            <div className="user-info">
              <h1 className="username-display">{profileData.username}</h1>
              <div className="user-meta">
                <span className="player-id">ID: {profileData.playerId}</span>
                <span className="join-date">Joined {formatDate(profileData.joinDate)}</span>
              </div>
            </div>
          </div>
          <div className="quick-stats">
            <div className="quick-stat">
              <div className="stat-number">{profileData.totalGames}</div>
              <div className="stat-label">Games Played</div>
            </div>
          </div>
        </div>

        {/* Personal Records Section */}
        <div className="records-section">
          <h2 className="section-title">Personal Records</h2>
          <div className="records-grid">
            <div className="record-card">
              <div className="record-icon">
                <svg width="32" height="32" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                    <path d="M16.5 18.75h-9m9 0a3 3 0 0 1 3 3h-15a3 3 0 0 1 3-3m9 0v-3.375c0-.621-.503-1.125-1.125-1.125h-.871M7.5 18.75v-3.375c0-.621.504-1.125 1.125-1.125h.872m5.007 0H9.497m5.007 0a7.454 7.454 0 0 1-.982-3.172M9.497 14.25a7.454 7.454 0 0 0 .981-3.172M5.25 4.236c-.982.143-1.954.317-2.916.52A6.003 6.003 0 0 0 7.73 9.728M5.25 4.236V4.5c0 2.108.966 3.99 2.48 5.228M5.25 4.236V2.721C7.456 2.41 9.71 2.25 12 2.25c2.291 0 4.545.16 6.75.47v1.516M7.73 9.728a6.726 6.726 0 0 0 2.748 1.35m8.272-6.842V4.5c0 2.108-.966 3.99-2.48 5.228m2.48-5.492a46.32 46.32 0 0 1 2.916.52 6.003 6.003 0 0 1-5.395 4.972m0 0a6.726 6.726 0 0 1-2.749 1.35m0 0a6.772 6.772 0 0 1-3.044 0" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
              <div className="record-content">
                <div className="record-value">{profileData.personalRecords.highestScore}%</div>
                <div className="record-label">Highest Score</div>
              </div>
            </div>
            <div className="record-card">
              <div className="record-icon">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
                </svg>
              </div>
              <div className="record-content">
                <div className="record-value">{profileData.personalRecords.bestAccuracy}%</div>
                <div className="record-label">Best Accuracy</div>
              </div>
            </div>
            <div className="record-card">
              <div className="record-icon">
                <svg width="32" height="32" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                    <path d="M15.362 5.214A8.252 8.252 0 0 1 12 21 8.25 8.25 0 0 1 6.038 7.047 8.287 8.287 0 0 0 9 9.601a8.983 8.983 0 0 1 3.361-6.867 8.21 8.21 0 0 0 3 2.48Z" stroke-linecap="round" stroke-linejoin="round"/>
                    <path d="M12 18a3.75 3.75 0 0 0 .495-7.468 5.99 5.99 0 0 0-1.925 3.547 5.975 5.975 0 0 1-2.133-1.001A3.75 3.75 0 0 0 12 18Z" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
              <div className="record-content">
                <div className="record-value">{profileData.personalRecords.longestStreak}</div>
                <div className="record-label">Win Streak</div>
              </div>
            </div>
            <div className="record-card">
              <div className="record-icon">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8z"/>
                  <path d="M12.5 7H11v6l5.25 3.15.75-1.23-4.5-2.67z"/>
                </svg>
              </div>
              <div className="record-content">
                <div className="record-value">{profileData.personalRecords.fastestGame}</div>
                <div className="record-label">Fastest Game</div>
              </div>
            </div>
          </div>
        </div>

        {/* Game History Section */}
        <div className="history-section">
          <h2 className="section-title">Recent Games</h2>
          <div className="games-list">
            {profileData.recentGames.length === 0 ? (
              <div className="no-games">
                <p>No games played yet</p>
              </div>
            ) : (
              profileData.recentGames.map((game) => (
                <div key={game.id} className="game-item">
                  <div className="game-category">
                    <span className="category-badge">{game.category}</span>
                  </div>
                  <div className="game-stats">
                    <div className="game-score">
                      <span className="score-value">{game.score}%</span>
                      <span className="score-label">Score</span>
                    </div>
                    <div className="game-accuracy">
                      <span className="accuracy-value">{game.accuracy}%</span>
                      <span className="accuracy-label">Accuracy</span>
                    </div>
                    <div className="game-duration">
                      <span className="duration-value">{game.duration}</span>
                      <span className="duration-label">Time</span>
                    </div>
                  </div>
                  <div className="game-date">
                    {formatDate(game.date)}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        <button className="back-button" onClick={onBack}>
          <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
            <path d="M8 0L0 8l8 8V0z"/>
          </svg>
          Back
        </button>
      </div>
    </div>
  );
};

export default Profile;
