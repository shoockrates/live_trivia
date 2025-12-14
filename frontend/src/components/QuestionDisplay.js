import React, { useState, useEffect, useRef } from 'react';
import './QuestionDisplay.css';

const QuestionDisplay = ({
    question,
    answers,
    correctIndexes = [],
    onAnswerSelect,
    onNext,
    onTimerExpire,
    currentIndex = 0,
    totalQuestions = 5,
    correctCount = 0,
    wrongCount = 0,
    revealed = false,
    questionIn = true,

    // Multiplayer props
    isLastQuestion = false,
    isMultiplayer = false,
    isHost = false,
    answeredPlayers = 0,
    totalPlayers = 1,
    currentScore = 0,
}) => {
    const [selectedAnswers, setSelectedAnswers] = useState([]);
    const [isAnswered, setIsAnswered] = useState(false);
    const [timeLeft, setTimeLeft] = useState(30);
    const timerExpiredRef = useRef(false); // Track if timer already expired

    const isMultiChoice = correctIndexes.length > 1;

    // Reset state when question changes
    useEffect(() => {
        setSelectedAnswers([]);
        setIsAnswered(false);
        setTimeLeft(30);
        timerExpiredRef.current = false; // Reset timer flag
    }, [currentIndex]);

    // FIXED: Timer logic - prevent double triggering
    useEffect(() => {
        if (isAnswered || timeLeft <= 0 || timerExpiredRef.current) return;

        const timer = setTimeout(() => {
            setTimeLeft(prev => {
                if (prev <= 1) {
                    // Mark that timer has expired to prevent double triggering
                    if (!timerExpiredRef.current) {
                        timerExpiredRef.current = true;

                        console.log('Timer expired. IsMultiplayer:', isMultiplayer, 'IsHost:', isHost);

                        if (isMultiplayer) {
                            // In multiplayer, only host calls the expire callback
                            if (isHost && onTimerExpire) {
                                console.log('Host calling onTimerExpire');
                                onTimerExpire();
                            } else {
                                console.log('Non-host waiting for host to advance');
                            }
                        } else {
                            // Single player - auto submit
                            if (!isAnswered) {
                                handleSubmit();
                            }
                        }
                    }
                    return 0;
                }
                return prev - 1;
            });
        }, 1000);

        return () => clearTimeout(timer);
    }, [timeLeft, isAnswered, isMultiplayer, isHost, onTimerExpire]);

    const handleAnswerClick = (idx) => {
        if (isAnswered) return;

        if (isMultiChoice) {
            setSelectedAnswers(prev =>
                prev.includes(idx)
                    ? prev.filter(i => i !== idx)
                    : [...prev, idx]
            );
        } else {
            setSelectedAnswers([idx]);
        }
    };

    const handleSubmit = () => {
        // Prevent double submission
        if (isAnswered) {
            console.log('Already answered, skipping submit');
            return;
        }

        console.log('handleSubmit called. Selected answers:', selectedAnswers);
        setIsAnswered(true);

        const finalAnswer = selectedAnswers.length > 0
            ? [...selectedAnswers].sort((a, b) => a - b)
            : []; // Empty array = wrong answer

        onAnswerSelect(finalAnswer, timeLeft);
    };

    // Auto-advance only in single player mode
    useEffect(() => {
        if (!isMultiplayer && isAnswered) {
            const timer = setTimeout(() => {
                if (currentIndex < totalQuestions - 1) onNext();
            }, 2000);
            return () => clearTimeout(timer);
        }
    }, [isAnswered, isMultiplayer, currentIndex, totalQuestions, onNext]);

    const getAnswerColor = (i) => ['#2ecc71', '#3498db', '#e74c3c', '#f39c12'][i] || '#6e7bff';
    const getAnswerIcon = (i) => ['A', 'B', 'C', 'D'][i] || '?';

    return (
        <div className="question-display-container">
            <div className={`question-card ${questionIn ? 'question-in' : ''}`}>
                {/* Header */}
                <div className="question-header">
                    <div className="progress-section">
                        <div className="progress-bar">
                            <div
                                className="progress-fill"
                                style={{ width: `${((currentIndex + 1) / totalQuestions) * 100}%` }}
                            />
                        </div>
                        <div className="progress-text">
                            Question {currentIndex + 1} of {totalQuestions}
                        </div>
                    </div>
                    {isMultiplayer && (
                        <div className="multiplayer-status">
                            <div className="players-progress">
                                {answeredPlayers} / {totalPlayers} players answered
                            </div>
                            {isHost && (
                                <div className="host-indicator">
                                    Host
                                </div>
                            )}
                        </div>
                    )}
                    <div className="timer-section">
                        <div className={`timer ${timeLeft <= 10 ? 'warning' : ''}`}>
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm.5-13h-1v6l5.25 3.15.75-1.23-4.5-2.67z" />
                            </svg>
                            {timeLeft}s
                        </div>
                    </div>
                </div>

                <h2 className="question-text">{question}</h2>

                {isMultiChoice && (
                    <div className="multi-choice-indicator">
                        <div className="rectangle-indicator" />
                        Select all that apply
                    </div>
                )}

                <div className="answers-container">
                    {answers.map((ans, idx) => {
                        const selected = selectedAnswers.includes(idx);
                        const correct = revealed && correctIndexes.includes(idx);
                        const wrong = revealed && selected && !correctIndexes.includes(idx);

                        return (
                            <button
                                key={idx}
                                className={`answer-option ${selected ? 'selected' : ''} ${revealed ? (correct ? 'correct' : wrong ? 'wrong' : 'faded') : ''
                                    }`}
                                onClick={() => handleAnswerClick(idx)}
                                disabled={isAnswered}
                            >
                                <div className="answer-icon" style={{ '--answer-color': getAnswerColor(idx) }}>
                                    {getAnswerIcon(idx)}
                                </div>
                                <div className="answer-text">{ans}</div>
                            </button>
                        );
                    })}
                </div>

                {/* FOOTER */}
                <div className="question-footer">
                    <div className="stats-section">
                        <div className="stat-item correct">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
                            </svg>
                            <span>{correctCount}</span>
                        </div>
                        <div className="stat-item wrong">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z" />
                            </svg>
                            <span>{wrongCount}</span>
                        </div>
                        {isMultiplayer && (
                            <div className="stat-item score">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                                    <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
                                </svg>
                                <span>{currentScore} pts</span>
                            </div>
                        )}
                    </div>

                    {/* HOST: Next Question */}
                    {isMultiplayer && isHost && (
                        <>
                            {/* Submit Answer Button */}
                            {!isAnswered && (
                                <button
                                    className="next-button"
                                    onClick={handleSubmit}
                                    disabled={selectedAnswers.length === 0}
                                >
                                    {isMultiChoice ? 'Submit Answers' : 'Submit Answer'}
                                </button>
                            )}

                            {/* Next Question Button */}
                            {isAnswered && (
                                <button className="next-button host-next-button" onClick={onNext}>
                                    {isLastQuestion ? 'Finish Quiz' : `Next Question (${answeredPlayers}/${totalPlayers})`}
                                </button>
                            )}
                        </>
                    )}

                    {/* NON-HOST: Submit → Waiting */}
                    {isMultiplayer && !isHost && (
                        <>
                            {!isAnswered && (
                                <button
                                    className="next-button"
                                    onClick={handleSubmit}
                                    disabled={selectedAnswers.length === 0}
                                >
                                    {isMultiChoice ? 'Submit Answers' : 'Submit Answer'}
                                </button>
                            )}
                            {isAnswered && (
                                <div style={{ color: '#88e0ff', fontSize: '16px', fontWeight: '600' }}>
                                    Waiting for host… ({answeredPlayers}/{totalPlayers})
                                </div>
                            )}
                        </>
                    )}

                    {/* SINGLE PLAYER: Submit button */}
                    {!isMultiplayer && !isAnswered && (
                        <button
                            className="next-button"
                            onClick={handleSubmit}
                            disabled={selectedAnswers.length === 0}
                        >
                            {isMultiChoice ? 'Submit Answers' : 'Submit Answer'}
                        </button>
                    )}

                    {/* SINGLE PLAYER: SKIP WAIT WITH NEXT BUTTON */}
                    {!isMultiplayer && isAnswered && currentIndex < totalQuestions - 1 && (
                        <button className="next-button" onClick={onNext}>
                            Next Question →
                        </button>
                    )}

                    {/* SINGLE PLAYER: Last question → Finish */}
                    {!isMultiplayer && isAnswered && currentIndex >= totalQuestions - 1 && (
                        <button className="next-button" onClick={onNext}>
                            Finish Quiz
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
};

QuestionDisplay.defaultProps = {
    correctIndexes: [],
    revealed: false,
    questionIn: true,
    isMultiplayer: false,
    isHost: false,
    answeredPlayers: 0,
    totalPlayers: 1,
    currentScore: 0,
};

export default QuestionDisplay;
