import { FileImage } from "lucide-react";
import ScanDetail from "./ScanDetail";

export default function ScansTab({ scans, selectedScan, onSelectScan, patient }) {
  if (scans.length === 0) {
    return (
      <div className="emptyPanel">
        <FileImage size={42} />
        <h3>No scans yet</h3>
        <p>Use the Upload new scan tab to classify this patient's first MRI.</p>
      </div>
    );
  }

  return (
    <div className="scanWorkspace">
      <div className="scanList">
        {scans.map((scan) => (
          <button
            className={`scanListItem ${scan.id === selectedScan?.id ? "active" : ""}`}
            key={scan.id}
            onClick={() => onSelectScan(scan.id)}
            type="button"
          >
            <img src={scan.imageUrl} alt={scan.originalFileName} />
            <span>
              <strong>{scan.predictedLabel}</strong>
              <small>{new Date(scan.createdAt).toLocaleDateString()} | {Math.round(scan.confidence * 100)}% confidence</small>
            </span>
          </button>
        ))}
      </div>
      <ScanDetail scan={selectedScan || scans[0]} patient={patient} />
    </div>
  );
}
