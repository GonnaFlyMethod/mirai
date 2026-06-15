export default function PatientsPanel({ patients, selectedPatientId, onSelect }) {
  if (patients.length === 0) {
    return <div className="emptyInline">No patients match your search.</div>;
  }

  return (
    <div className="patientCards">
      {patients.map((patient) => (
        <button
          className={`patientCard ${patient.id === selectedPatientId ? "active" : ""}`}
          key={patient.id}
          onClick={() => onSelect(patient.id)}
          type="button"
        >
          <span className="avatar">{patient.firstName?.[0]}{patient.lastName?.[0]}</span>
          <span className="patientCardMain">
            <strong>{patient.firstName} {patient.lastName}</strong>
            <span>
              {patient.scanCount} scans
              {patient.latestScan ? ` | latest: ${patient.latestScan.predictedLabel}` : ""}
            </span>
          </span>
        </button>
      ))}
    </div>
  );
}
