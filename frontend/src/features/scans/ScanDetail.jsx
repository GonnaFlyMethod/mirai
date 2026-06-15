import { CircleAlert } from "lucide-react";
import ProbabilityList from "./ProbabilityList";

export default function ScanDetail({ scan, patient }) {
  if (!scan) {
    return null;
  }

  return (
    <article className="scanDetail">
      <div className="resultHeader">
        <div>
          <span className="eyebrow">Classification result</span>
          <h3>{scan.predictedLabel}</h3>
          <p>{patient.firstName} {patient.lastName} | {new Date(scan.createdAt).toLocaleString()}</p>
        </div>
        <span className="confidence">{Math.round(scan.confidence * 100)}%</span>
      </div>
      <img src={scan.imageUrl} alt={scan.originalFileName} />
      <div className="scanMeta">
        <span>{scan.originalFileName}</span>
        <span>{scan.analysisStatus}</span>
        <span>{scan.analyzerVersion}</span>
      </div>
      {scan.errorMessage && <div className="message inline"><CircleAlert size={18} /> {scan.errorMessage}</div>}
      <ProbabilityList probabilities={scan.probabilities} />
    </article>
  );
}
