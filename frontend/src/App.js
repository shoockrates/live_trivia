import React, { useEffect, useMemo, useState } from 'react';
import './App.css';
import Login from './components/Login';
import Register from './components/Register';
import UserDropdown from './components/UserDropdown';

function App() {
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [categories, setCategories] = useState([]);
  const [questions, setQuestions] = useState([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [isAnimatingBack, setIsAnimatingBack] = useState(false);
  const [selectedAnswerIndex, setSelectedAnswerIndex] = useState(null);
  const [revealed, setRevealed] = useState(false);
  const [correctCount, setCorrectCount] = useState(0);
  const [wrongCount, setWrongCount] = useState(0);
  const [gameStartedAt, setGameStartedAt] = useState(null);
  const [gameEndedAt, setGameEndedAt] = useState(null);
  const [gameFinished, setGameFinished] = useState(false);
  const [questionIn, setQuestionIn] = useState(false);
  const [resultsIn, setResultsIn] = useState(false);
  
  // Authentication states
  const [currentView, setCurrentView] = useState('auth');
  const [user, setUser] = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  // Backend base URL
  const API_BASE = useMemo(() => 'http://localhost:5216', []);

  // Check if user is already logged in on app start
  useEffect(() => {
    const token = localStorage.getItem('token');
    const username = localStorage.getItem('username');
    const playerId = localStorage.getItem('playerId');

    if (token && username) {
      setUser({ username, playerId, token });
      setIsAuthenticated(true);
      setCurrentView('game');
    }
  }, []);

  // Add authentication header to all fetch requests
  useEffect(() => {
    const originalFetch = window.fetch;
    window.fetch = async (...args) => {
      const [url, options = {}] = args;
      const token = localStorage.getItem('token');
      
      if (token && url.startsWith(API_BASE)) {
        options.headers = {
          ...options.headers,
          'Authorization': `Bearer ${token}`
        };
      }
      
      return originalFetch(url, options);
    };

    return () => {
      window.fetch = originalFetch;
    };
  }, [API_BASE]);

  // Authentication handlers
  const handleLogin = (userData) => {
    setUser(userData);
    setIsAuthenticated(true);
    setCurrentView('game');
  };

  const handleRegister = (userData) => {
    setUser(userData);
    setIsAuthenticated(true);
    setCurrentView('game');
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    localStorage.removeItem('playerId');
    setUser(null);
    setIsAuthenticated(false);
    setCurrentView('auth');
    setSelectedCategory(null);
  };

  const showLogin = () => setCurrentView('login');
  const showRegister = () => setCurrentView('register');
  const showAuth = () => setCurrentView('auth');

  // Load all questions once to derive categories
  useEffect(() => {
    if (!isAuthenticated) return;

    const controller = new AbortController();
    const load = async () => {
      try {
        setError(null);
        const res = await fetch(`${API_BASE}/questions`, { signal: controller.signal });
        if (!res.ok) throw new Error(`Failed to load questions (${res.status})`);
        const all = await res.json();
        const valid = (Array.isArray(all) ? all : []).filter(q => {
          const t = (q.text || q.question || '').trim();
          const a = Array.isArray(q.answers || q.Answers) ? (q.answers || q.Answers) : [];
          const c = Array.isArray(q.correctAnswerIndexes || q.CorrectAnswerIndexes) ? (q.correctAnswerIndexes || q.CorrectAnswerIndexes) : [];
          return t.length > 0 && a.length > 0 && c.length > 0;
        });
        const unique = Array.from(new Set(valid.map(q => q.category))).sort((a, b) => a.localeCompare(b));
        setCategories(unique);
      } catch (e) {
        if (e.name !== 'AbortError') setError(e.message || 'Failed to load');
      }
    };
    load();
    return () => controller.abort();
  }, [API_BASE, isAuthenticated]);

  const handleSelect = (category) => {
    setSelectedCategory(category);
    setLoading(true);
    setError(null);
    setCurrentIndex(0);
    setSelectedAnswerIndex(null);
    setRevealed(false);
    setCorrectCount(0);
    setWrongCount(0);
    setGameFinished(false);
    setGameEndedAt(null);
    setGameStartedAt(Date.now());
    
    fetch(`${API_BASE}/questions/category/${encodeURIComponent(category)}`)
      .then(r => {
        if (!r.ok) throw new Error(`Failed to load ${category} (${r.status})`);
        return r.json();
      })
      .then(data => {
        const arr = Array.isArray(data) ? data : [];
        const filtered = arr.filter(q => {
          const t = (q.text || q.question || '').trim();
          const a = Array.isArray(q.answers || q.Answers) ? (q.answers || q.Answers) : [];
          const c = Array.isArray(q.correctAnswerIndexes || q.CorrectAnswerIndexes) ? (q.correctAnswerIndexes || q.CorrectAnswerIndexes) : [];
          return t.length > 0 && a.length > 0 && c.length > 0;
        });
        setQuestions(filtered);
      })
      .catch(err => setError(err.message || 'Failed to load'))
      .finally(() => setLoading(false));
  };

  const handleBack = () => {
    setIsAnimatingBack(true);
    setTimeout(() => {
      setSelectedCategory(null);
      setIsAnimatingBack(false);
    }, 220);
  };

  const currentQuestion = questions[currentIndex] || {};
  const questionText = (currentQuestion.text || currentQuestion.question || '').trim();
  const rawAnswers = currentQuestion.answers || currentQuestion.Answers || [];
  const answers = Array.isArray(rawAnswers) ? rawAnswers.slice(0, 4) : [];
  const correctIdxs = useMemo(() => {
    const arr = currentQuestion.correctAnswerIndexes || currentQuestion.CorrectAnswerIndexes || [];
    return Array.isArray(arr) ? arr : [];
  }, [currentQuestion]);

  // Trigger enter animation on question change
  useEffect(() => {
    setQuestionIn(false);
    const t = setTimeout(() => setQuestionIn(true), 20);
    return () => clearTimeout(t);
  }, [currentIndex, selectedCategory]);

  // Trigger enter animation for results view
  useEffect(() => {
    if (gameFinished) {
      setResultsIn(false);
      const t = setTimeout(() => setResultsIn(true), 20);
      return () => clearTimeout(t);
    } else {
      setResultsIn(false);
    }
  }, [gameFinished]);

  const selectAnswer = (idx) => {
    if (revealed) return;
    setSelectedAnswerIndex(idx);
    const isCorrect = correctIdxs.includes(idx);
    if (isCorrect) {
      setCorrectCount(c => c + 1);
    } else {
      setWrongCount(w => w + 1);
    }
    setRevealed(true);
  };

  const goNext = () => {
    if (currentIndex >= questions.length - 1) {
      if (!gameFinished) {
        setGameFinished(true);
        setGameEndedAt(Date.now());
      }
      return;
    }
    setCurrentIndex(i => i + 1);
    setSelectedAnswerIndex(null);
    setRevealed(false);
  };

  const restartGame = () => {
    if (!selectedCategory) return;
    handleSelect(selectedCategory);
  };

  const percent = useMemo(() => {
    const total = questions.length || 0;
    if (total === 0) return 0;
    return Math.round((correctCount / total) * 100);
  }, [correctCount, questions.length]);

  const getGrade = (pct, mistakes) => {
    if (mistakes === 0) return 'A+';
    if (pct >= 90) return 'A';
    if (pct >= 80) return 'B';
    if (pct >= 70) return 'C';
    if (pct >= 60) return 'D';
    return 'F';
  };

  const gameDurationMs = gameStartedAt && gameEndedAt ? (gameEndedAt - gameStartedAt) : null;
  const formatDuration = (ms) => {
    if (!ms || ms < 0) return '0:00';
    const totalSeconds = Math.floor(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  // Render authentication views
  if (!isAuthenticated) {
    return (
      <div className="App">
        {currentView === 'auth' && (
          <div className="TriviaContainer">
            <div className="HeroCard">
              <h1 className="HeroTitle">Live Trivia</h1>
              <p className="HeroSubtitle">Test your knowledge and challenge friends</p>
              <div className="ButtonsGrid">
                <button 
                  className="TriviaButton gradient" 
                  onClick={showLogin}
                >
                  Sign In
                </button>
                <button 
                  className="TriviaButton" 
                  onClick={showRegister}
                >
                  Create Account
                </button>
              </div>
            </div>
          </div>
        )}
        
        {currentView === 'login' && (
          <Login 
            onLogin={handleLogin}
            onSwitchToRegister={showRegister}
          />
        )}
        
        {currentView === 'register' && (
          <Register 
            onRegister={handleRegister}
            onSwitchToLogin={showLogin}
          />
        )}
      </div>
    );
  }

  // Render main game for authenticated users
  return (
    <div className="App">
      <div className="TriviaContainer">
        {/* Logout button */}
        <div style={{ 
            position: 'absolute', 
            top: '20px', 
            right: '20px', 
            zIndex: 1000 
            }}>
            <UserDropdown 
                user={user} 
                onLogout={handleLogout} 
            />
            </div>

        {!selectedCategory && (
          <div className="HeroCard">
            <h1 className="HeroTitle">Trivia Game</h1>
            <div className="ButtonsGrid minimal">
              {categories.map((cat) => (
                <button
                  key={cat}
                  className="TriviaButton minimal gradient"
                  onClick={() => handleSelect(cat)}
                >
                  {cat}
                </button>
              ))}
            </div>
            {error && (
              <p className="Hint" style={{ color: '#ff6b6b' }}>{error}</p>
            )}
          </div>
        )}

        {selectedCategory && (
          <div className={`SelectionStage ${isAnimatingBack ? 'shrinkOut' : 'growIn'}`}>
            <h1 className="CategoryTitle minimalTitle">{selectedCategory}</h1>
            {loading && (
              <div className="QuestionPlaceholder minimalCard">
                <p>Loading questions…</p>
              </div>
            )}
            {!loading && error && (
              <div className="QuestionPlaceholder minimalCard">
                <p style={{ color: '#ff6b6b' }}>{error}</p>
              </div>
            )}
            {!loading && !error && questions.length === 0 && (
              <div className="QuestionPlaceholder minimalCard">
                <p>No questions found for "{selectedCategory}".</p>
              </div>
            )}
            {!loading && !error && questions.length > 0 && !gameFinished && (
              <div
                className="QuestionCard minimalCard"
                style={{
                  opacity: questionIn ? 1 : 0,
                  transform: questionIn ? 'scale(1)' : 'scale(0.98)',
                  transition: 'opacity 360ms ease, transform 360ms ease'
                }}
              >
                {(() => {
                  const colors = [
                    { name: 'green', color: '#2ecc71' },
                    { name: 'blue', color: '#3498db' },
                    { name: 'red', color: '#e74c3c' },
                    { name: 'yellow', color: '#f1c40f' }
                  ];
                  const progressPct = ((currentIndex + (revealed ? 1 : 0)) / questions.length) * 100;
                  return (
                    <>
                      <div className="QuestionText" style={{ marginBottom: 12, fontSize: 20, fontWeight: 600 }}>
                        {questionText}
                      </div>
                      <div className="AnswersGrid">
                        {colors.map((c, idx) => {
                          const exists = Boolean(answers[idx]);
                          const isSelected = selectedAnswerIndex === idx;
                          const isCorrect = correctIdxs.includes(idx);
                          const stateClass = revealed
                            ? (isCorrect
                                ? 'answer-correct'
                                : (isSelected ? 'answer-selected-incorrect' : 'answer-incorrect'))
                            : '';
                          const animStyle = {
                            opacity: questionIn ? 1 : 0,
                            transform: questionIn ? 'translateY(0) scale(1)' : 'translateY(6px) scale(0.98)',
                            transition: 'opacity 420ms ease, transform 420ms ease, background 420ms ease, box-shadow 420ms ease, color 420ms ease, filter 500ms ease',
                            transitionDelay: `${idx * 60}ms`
                          };
                          return (
                            <button
                              key={c.name}
                              className={`TriviaButton minimal btn-${c.name} ${stateClass}`}
                              disabled={!exists || revealed}
                              onClick={() => selectAnswer(idx)}
                              aria-label={`Answer ${idx + 1}`}
                              style={{ ...animStyle }}
                            >
                              {answers[idx] || '—'}
                            </button>
                          );
                        })}
                      </div>
                      <div style={{ marginTop: 12 }}>
                        <div className="minimalCard" style={{ height: 6, padding: 0 }}>
                          <div style={{ width: `${progressPct}%`, height: 6, background: '#3498db', transition: 'width 300ms ease' }} />
                        </div>
                      </div>
                      <div style={{ marginTop: 12, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span className="Hint">{currentIndex + 1} / {questions.length}</span>
                        <div style={{ display: 'flex', gap: 8 }}>
                          <span className="Hint" style={{ color: '#2ecc71' }}>✔ {correctCount}</span>
                          <span className="Hint" style={{ color: '#e74c3c' }}>✖ {wrongCount}</span>
                        </div>
                        <button
                          className="BackButton"
                          onClick={goNext}
                          disabled={!revealed}
                        >
                          {currentIndex >= questions.length - 1 ? 'Finish' : 'Next'}
                        </button>
                      </div>
                    </>
                  );
                })()}
              </div>
            )}
            {!loading && !error && questions.length > 0 && gameFinished && (
              <div
                className="QuestionCard minimalCard"
                style={{
                  opacity: resultsIn ? 1 : 0,
                  transform: resultsIn ? 'scale(1)' : 'scale(0.98)',
                  transition: 'opacity 360ms ease, transform 360ms ease'
                }}
              >
                <div className="QuestionText" style={{ marginBottom: 12, fontSize: 22, fontWeight: 700 }}>
                  Results
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 12 }}>
                  <div className="minimalCard" style={{ padding: 12 }}>
                    <div className="Hint">Correct</div>
                    <div style={{ fontSize: 24, color: '#2ecc71', fontWeight: 700 }}>{correctCount}</div>
                  </div>
                  <div className="minimalCard" style={{ padding: 12 }}>
                    <div className="Hint">Wrong</div>
                    <div style={{ fontSize: 24, color: '#e74c3c', fontWeight: 700 }}>{wrongCount}</div>
                  </div>
                  <div className="minimalCard" style={{ padding: 12 }}>
                    <div className="Hint">Percent</div>
                    <div style={{ fontSize: 24, fontWeight: 700 }}>{percent}%</div>
                  </div>
                  <div className="minimalCard" style={{ padding: 12 }}>
                    <div className="Hint">Time</div>
                    <div style={{ fontSize: 24, fontWeight: 700 }}>{formatDuration(gameDurationMs)}</div>
                  </div>
                </div>
                <div className="minimalCard" style={{ padding: 16, display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: 12 }}>
                  <div style={{ fontSize: 28, fontWeight: 800 }}>
                    Grade: <span style={{ color: percent >= 60 ? '#2ecc71' : '#e74c3c' }}>{getGrade(percent, wrongCount)}</span>
                  </div>
                </div>
                <div style={{ display: 'flex', gap: 8, justifyContent: 'space-between' }}>
                  <button className="BackButton" onClick={handleBack}>Back</button>
                  <button className="BackButton" onClick={restartGame}>Play again</button>
                </div>
              </div>
            )}
            <button className="BackButton" onClick={handleBack} aria-label="Back to categories">
              Back
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
