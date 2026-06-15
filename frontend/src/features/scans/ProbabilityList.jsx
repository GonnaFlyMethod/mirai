export default function ProbabilityList({ probabilities }) {
  const entries = Object.entries(probabilities || {}).sort((a, b) => b[1] - a[1]);

  if (entries.length === 0) {
    return <p className="muted">No class probabilities available.</p>;
  }

  return (
    <div className="probabilities">
      {entries.map(([label, value]) => (
        <div className="probability" key={label}>
          <div className="probabilityLabel">
            <span>{label}</span>
            <span>{Math.round(value * 100)}%</span>
          </div>
          <div className="bar"><span style={{ width: `${Math.max(0, Math.min(100, value * 100))}%` }} /></div>
        </div>
      ))}
    </div>
  );
}
