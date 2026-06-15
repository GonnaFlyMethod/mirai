import { Trash2, Upload, UserRound } from "lucide-react";
import ScansTab from "../scans/ScansTab";
import UploadTab from "../scans/UploadTab";
import DetailsTab from "./DetailsTab";

export default function PatientWorkspace({
  patient,
  scans,
  selectedScan,
  activeTab,
  setActiveTab,
  scanFile,
  setScanFile,
  onSelectScan,
  onAnalyzeScan,
  onDelete,
  busy
}) {
  if (!patient) {
    return (
      <section className="patientWorkspace emptyWorkspace">
        <UserRound size={46} />
        <h2>Select a patient to start</h2>
        <p>Choose a patient from the top. Their scan history, classification results, and upload action will open here.</p>
      </section>
    );
  }

  return (
    <section className="patientWorkspace">
      <header className="patientHeader">
        <div className="patientIdentity">
          <span className="avatar large">{patient.firstName?.[0]}{patient.lastName?.[0]}</span>
          <div>
            <span className="eyebrow">Active patient</span>
            <h2>{patient.firstName} {patient.lastName}</h2>
            <p>{patient.email || "No email"} | {patient.phoneNumber || "No phone"}</p>
          </div>
        </div>
        <div className="patientActions">
          <button className="secondary iconButton" onClick={() => setActiveTab("upload")}>
            <Upload size={17} />
            Upload scan
          </button>
          <button className="danger iconOnly" disabled={busy} onClick={() => onDelete(patient.id)} title="Delete patient">
            <Trash2 size={17} />
          </button>
        </div>
      </header>

      <nav className="tabs" aria-label="Patient workspace tabs">
        <button className={activeTab === "scans" ? "active" : ""} onClick={() => setActiveTab("scans")} type="button">
          Scans <span>{scans.length}</span>
        </button>
        <button className={activeTab === "upload" ? "active" : ""} onClick={() => setActiveTab("upload")} type="button">
          Upload new scan
        </button>
        <button className={activeTab === "details" ? "active" : ""} onClick={() => setActiveTab("details")} type="button">
          Patient details
        </button>
      </nav>

      {activeTab === "scans" && (
        <ScansTab scans={scans} selectedScan={selectedScan} onSelectScan={onSelectScan} patient={patient} />
      )}

      {activeTab === "upload" && (
        <UploadTab patient={patient} scanFile={scanFile} setScanFile={setScanFile} onSubmit={onAnalyzeScan} busy={busy} />
      )}

      {activeTab === "details" && (
        <DetailsTab patient={patient} />
      )}
    </section>
  );
}
