import React, { useState } from "react";
import "./AddQuestionForm.css";

const categories = [
  "Science",
  "History",
  "Geography",
  "Sports",
  "Movies",
  "Music",
  "Literature",
  "Art",
  "Arts & Literature",
  "Technology",
  "Mathematics",
  "Biology",
  "Chemistry",
  "Physics",
  "Astronomy",
  "Politics",
  "Food",
  "Travel",
  "Nature",
  "Animals",
  "Space",
];

const getCategoryIcon = (category) => {
  const icons = {
    Science: "🔬",
    History: "📚",
    Geography: "🌍",
    Sports: "⚽",
    Movies: "🎬",
    Music: "🎵",
    Literature: "📖",
    Art: "🎨",
    "Arts & Literature": "🎭",
    Technology: "💻",
    Mathematics: "🔢",
    Biology: "🧬",
    Chemistry: "⚗️",
    Physics: "⚛️",
    Astronomy: "🌌",
    Politics: "🏛️",
    Food: "🍕",
    Travel: "✈️",
    Nature: "🌿",
    Animals: "🐾",
    Space: "🚀",
  };
  return icons[category] || "🧠";
};

const AddQuestionForm = ({ onSubmit, onBack, user, onSuccess }) => {
  const [category, setCategory] = useState("");
  const [difficulty, setDifficulty] = useState("");
  const [questionType, setQuestionType] = useState("single");
  const [questionText, setQuestionText] = useState("");
  const [answers, setAnswers] = useState([
    { text: "", correct: false },
    { text: "", correct: false },
    { text: "", correct: false },
    { text: "", correct: false },
  ]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [fadeOut, setFadeOut] = useState(false);

  const handleAnswerChange = (index, value) => {
    const newAnswers = [...answers];
    newAnswers[index].text = value;
    setAnswers(newAnswers);
  };

  const handleCorrectChange = (index) => {
    const newAnswers = [...answers];
    if (questionType === "single") {
      newAnswers.forEach((a, i) => (a.correct = i === index));
    } else {
      newAnswers[index].correct = !newAnswers[index].correct;
    }
    setAnswers(newAnswers);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!category) return setError("Please select a category.");
    if (!difficulty) return setError("Please select a difficulty.");
    if (!questionText.trim())
      return setError("Please enter the question text.");
    if (answers.some((a) => !a.text.trim()))
      return setError("Please fill all answer fields.");
    if (!answers.some((a) => a.correct))
      return setError("Please mark at least one correct answer.");
    setError("");

    const payload = {
      category,
      difficulty,
      type: questionType,
      text: questionText,
      answers: answers.map((a) => a.text),
      correctAnswerIndexes: answers
        .map((a, i) => (a.correct ? i : null))
        .filter((i) => i !== null),
    };

    try {
      const res = await fetch("http://localhost:5216/questions/submit", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!res.ok) throw new Error("Failed to submit question");

      // Reset form
      setCategory("");
      setDifficulty("");
      setQuestionType("single");
      setQuestionText("");
      setAnswers([
        { text: "", correct: false },
        { text: "", correct: false },
        { text: "", correct: false },
        { text: "", correct: false },
      ]);

      onSuccess();
      setSuccess("Question submitted successfully!");
      setFadeOut(false);

      // fade out success message after 2s
      setTimeout(() => {
        setFadeOut(true);
      }, 2000);

      setTimeout(() => {
        setSuccess("");
        setFadeOut(false);
      }, 2000);
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <h2 className="auth-title">Add Question</h2>
        <p className="auth-subtitle">Fill out the form below</p>

        {error && <div className="error-message">{error}</div>}
        {success && (
          <div className={`success-message ${fadeOut ? "fade-out" : ""}`}>
            {success}
          </div>
        )}

        <div className="input-group">
          <label className="selector-text">Category</label>
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            style={{
              padding: "12px 14px",
              borderRadius: "12px",
              border: "1px solid rgba(156,200,255,0.25)",
              background: "rgba(10,14,19,0.6)",
              color: "#eaf2f8",
              fontSize: "15px",
              fontWeight: 500,
              appearance: "none",
            }}
          >
            <option value="">-- Select Category --</option>
            {categories.map((c) => (
              <option key={c} value={c}>
                {getCategoryIcon(c)} {c}
              </option>
            ))}
          </select>
        </div>

        <div className="input-group">
          <label className="selector-text">Difficulty</label>
          <select
            value={difficulty}
            onChange={(e) => setDifficulty(e.target.value)}
            style={{
              padding: "12px 14px",
              borderRadius: "12px",
              border: "1px solid rgba(156,200,255,0.25)",
              background: "rgba(10,14,19,0.6)",
              color: "#eaf2f8",
              fontSize: "15px",
              fontWeight: 500,
              appearance: "none",
              transition: "border-color 0.3s ease, box-shadow 0.3s ease",
            }}
            onMouseEnter={(e) => {
              e.target.style.borderColor = "rgba(110,123,255,0.5)";
              e.target.style.boxShadow = "0 0 0 3px rgba(110,123,255,0.1)";
            }}
            onMouseLeave={(e) => {
              e.target.style.borderColor = "rgba(156,200,255,0.25)";
              e.target.style.boxShadow = "none";
            }}
          >
            <option value="">-- Select Difficulty --</option>
            <option value="easy">Easy</option>
            <option value="medium">Medium</option>
            <option value="hard">Hard</option>
          </select>
        </div>

        <div className="input-group">
          <label className="selector-text">Question Type</label>
          <select
            value={questionType}
            onChange={(e) => setQuestionType(e.target.value)}
            style={{
              padding: "12px 14px",
              borderRadius: "12px",
              border: "1px solid rgba(156,200,255,0.25)",
              background: "rgba(10,14,19,0.6)",
              color: "#eaf2f8",
              fontSize: "15px",
              fontWeight: 500,
              appearance: "none",
            }}
          >
            <option value="single">Single Answer</option>
            <option value="multiple">Multiple Answers</option>
          </select>
        </div>

        <div className="input-group">
          <label className="selector-text">Question Text</label>
          <input
            type="text"
            placeholder="Enter question text"
            value={questionText}
            onChange={(e) => setQuestionText(e.target.value)}
          />
        </div>

        <div className="input-group">
          <label className="selector-text">Answers</label>
          {answers.map((a, i) => (
            <div
              key={i}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "10px",
                marginBottom: "10px",
              }}
            >
              <input
                type="text"
                placeholder={`Answer ${i + 1}`}
                value={a.text}
                onChange={(e) => handleAnswerChange(i, e.target.value)}
                style={{
                  flex: 1,
                  padding: "10px 12px",
                  borderRadius: "12px",
                  border: "1px solid rgba(156,200,255,0.25)",
                  background: "rgba(20,28,36,0.6)",
                  color: "#eaf2f8",
                  fontSize: "14px",
                }}
              />
              <label
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "6px",
                  color: "#eaf2f8",
                  fontWeight: 500,
                  cursor: "pointer",
                }}
              >
                <input
                  type={questionType === "single" ? "radio" : "checkbox"}
                  name="correctAnswer"
                  checked={a.correct}
                  onChange={() => handleCorrectChange(i)}
                  style={{
                    width: "18px",
                    height: "18px",
                    accentColor: "#6e7bff",
                    cursor: "pointer",
                  }}
                />
                Correct
              </label>
            </div>
          ))}
        </div>

        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            marginTop: "20px",
          }}
        >
          <button className="return-button" onClick={onBack}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M8 0L0 8l8 8V0z" />
            </svg>
            Back
          </button>
          <button className="add-question-button" onClick={handleSubmit}>
            Add Question
          </button>
        </div>
      </div>
    </div>
  );
};

export default AddQuestionForm;
