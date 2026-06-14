import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import {
  Activity,
  Brain,
  CalendarDays,
  CircleAlert,
  FileImage,
  Mail,
  MapPin,
  Phone,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  Upload,
  UserRound
} from "lucide-react";
import "./styles.css";

const emptyPatient = {
  firstName: "",
  lastName: "",
  dateOfBirth: "1990-01-01",
  gender: "",
  email: "",
  phoneNumber: "",
  address: ""
};

function App() {
  const [health, setHealth] = useState("checking");
  const [patients, setPatients] = useState([]);
  const [selectedPatientId, setSelectedPatientId] = useState("");
  const [selectedPatient, setSelectedPatient] = useState(null);
  const [scans, setScans] = useState([]);
  const [selectedScan, setSelectedScan] = useState(null);
  const [activeTab, setActiveTab] = useState("scans");
  const [patientForm, setPatientForm] = useState(emptyPatient);
  const [showPatientForm, setShowPatientForm] = useState(false);
  const [scanFile, setScanFile] = useState(null);
  const [query, setQuery] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    refreshAll();
  }, []);

  useEffect(() => {
    if (!selectedPatientId) {
      setSelectedPatient(null);
      setSelectedScan(null);
      return;
    }

    api(`/api/patients/${selectedPatientId}`)
      .then(setSelectedPatient)
      .catch((error) => setMessage(error.message));
  }, [selectedPatientId]);

  const scansByPatient = useMemo(() => {
    return scans.reduce((groups, scan) => {
      if (!scan.patientId) {
        return groups;
      }

      groups[scan.patientId] = groups[scan.patientId] || [];
      groups[scan.patientId].push(scan);
      return groups;
    }, {});
  }, [scans]);

  const selectedPatientScans = useMemo(() => {
    return selectedPatientId ? scansByPatient[selectedPatientId] || [] : [];
  }, [scansByPatient, selectedPatientId]);

  useEffect(() => {
    if (!selectedPatientId) {
      return;
    }

    const selectedStillBelongs = selectedScan?.patientId === selectedPatientId;
    if (!selectedStillBelongs) {
      setSelectedScan(selectedPatientScans[0] || null);
    }
  }, [selectedPatientId, selectedPatientScans, selectedScan?.patientId]);

  const enrichedPatients = useMemo(() => {
    return patients.map((patient) => {
      const patientScans = scansByPatient[patient.id] || [];
      const latestScan = patientScans[0] || null;

      return {
        ...patient,
        scanCount: patientScans.length,
        latestScan
      };
    });
  }, [patients, scansByPatient]);

  const filteredPatients = useMemo(() => {
    const normalized = query.trim().toLowerCase();

    if (!normalized) {
      return enrichedPatients;
    }

    return enrichedPatients.filter((patient) =>
      [
        patient.firstName,
        patient.lastName,
        patient.email,
        patient.phoneNumber,
        patient.address
      ].some((value) => String(value || "").toLowerCase().includes(normalized))
    );
  }, [enrichedPatients, query]);

  const stats = useMemo(() => {
    const highConfidence = scans.filter((scan) => scan.confidence >= 0.8).length;
    const patientsWithScans = new Set(scans.map((scan) => scan.patientId).filter(Boolean)).size;

    return {
      patients: patients.length,
      scans: scans.length,
      patientsWithScans,
      highConfidence
    };
  }, [patients.length, scans]);

  async function refreshAll() {
    setMessage("");
    try {
      const [healthResult, patientsResult, scansResult] = await Promise.all([
        api("/api/health"),
        api("/api/patients"),
        api("/api/scans")
      ]);
      setHealth(healthResult.status);
      setPatients(patientsResult);
      setScans(scansResult);
    } catch (error) {
      setHealth("offline");
      setMessage(error.message);
    }
  }

  function selectPatient(id) {
    setSelectedPatientId(id);
    setActiveTab("scans");
    setScanFile(null);
    setMessage("");
  }

  async function createPatient(event) {
    event.preventDefault();
    setBusy(true);
    setMessage("");

    try {
      const created = await api("/api/patients", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(patientForm)
      });
      setPatientForm(emptyPatient);
      setShowPatientForm(false);
      setSelectedPatientId(created.id);
      setActiveTab("details");
      await refreshAll();
    } catch (error) {
      setMessage(error.message);
    } finally {
      setBusy(false);
    }
  }

  async function deletePatient(id) {
    setBusy(true);
    setMessage("");

    try {
      await api(`/api/patients/${id}`, { method: "DELETE", empty: true });
      if (selectedPatientId === id) {
        setSelectedPatientId("");
        setSelectedScan(null);
      }
      await refreshAll();
    } catch (error) {
      setMessage(error.message);
    } finally {
      setBusy(false);
    }
  }

  async function analyzeScan(event) {
    event.preventDefault();

    if (!selectedPatientId) {
      setMessage("Select a patient first.");
      return;
    }

    if (!scanFile) {
      setMessage("Choose an MRI image first.");
      return;
    }

    setBusy(true);
    setMessage("");

    try {
      const form = new FormData();
      form.append("image", scanFile);
      form.append("patientId", selectedPatientId);

      const created = await api("/api/scans/analyze", {
        method: "POST",
        body: form
      });

      setScanFile(null);
      setSelectedScan(created);
      setActiveTab("scans");
      await refreshAll();
    } catch (error) {
      setMessage(error.message);
    } finally {
      setBusy(false);
    }
  }

  async function loadScan(id) {
    try {
      const scan = await api(`/api/scans/${id}`);
      setSelectedScan(scan);
      setActiveTab("scans");
    } catch (error) {
      setMessage(error.message);
    }
  }

  return (
    <main className="appShell">
      <header className="topbar">
        <div className="brand">
          <div className="brandMark"><Brain size={28} /></div>
          <div>
            <h1>Mirai</h1>
            <span>Patient MRI classification</span>
          </div>
        </div>
        <div className="toolbar">
          <span className={`status ${health === "ok" ? "online" : ""}`}><Activity size={16} /> API {health}</span>
          <button className="secondary iconButton" onClick={refreshAll} title="Refresh data">
            <RefreshCw size={17} />
            Refresh
          </button>
        </div>
      </header>

      {message && <div className="message"><CircleAlert size={18} /> {message}</div>}

      <section className="statsGrid">
        <Metric icon={<UserRound size={20} />} label="Patients" value={stats.patients} />
        <Metric icon={<FileImage size={20} />} label="Scans" value={stats.scans} />
        <Metric icon={<Brain size={20} />} label="Patients with scans" value={stats.patientsWithScans} />
        <Metric icon={<Activity size={20} />} label="High confidence" value={stats.highConfidence} />
      </section>

      <section className="workspace">
        <aside className="patientRail">
          <div className="railHeader">
            <div>
              <h2>Patients</h2>
              <span>{filteredPatients.length} shown</span>
            </div>
            <button className="iconOnly" onClick={() => setShowPatientForm((value) => !value)} title="Create patient">
              <Plus size={18} />
            </button>
          </div>

          {showPatientForm && (
            <PatientForm busy={busy} form={patientForm} setForm={setPatientForm} onSubmit={createPatient} />
          )}

          <div className="searchBox">
            <Search size={18} />
            <input placeholder="Search name, email, phone" value={query} onChange={(event) => setQuery(event.target.value)} />
          </div>

          <PatientsPanel
            patients={filteredPatients}
            selectedPatientId={selectedPatientId}
            onSelect={selectPatient}
          />
        </aside>

        <PatientWorkspace
          patient={selectedPatient}
          scans={selectedPatientScans}
          selectedScan={selectedScan}
          activeTab={activeTab}
          setActiveTab={setActiveTab}
          scanFile={scanFile}
          setScanFile={setScanFile}
          onSelectScan={loadScan}
          onAnalyzeScan={analyzeScan}
          onDelete={deletePatient}
          busy={busy}
        />
      </section>
    </main>
  );
}

function Metric({ icon, label, value }) {
  return (
    <article className="metric">
      <div className="metricIcon">{icon}</div>
      <div>
        <span>{label}</span>
        <strong>{value}</strong>
      </div>
    </article>
  );
}

function PatientForm({ busy, form, setForm, onSubmit }) {
  function update(field, value) {
    setForm((previous) => ({ ...previous, [field]: value }));
  }

  return (
    <form className="patientForm" onSubmit={onSubmit}>
      <div className="fieldRow">
        <input placeholder="First name" value={form.firstName} onChange={(event) => update("firstName", event.target.value)} required />
        <input placeholder="Last name" value={form.lastName} onChange={(event) => update("lastName", event.target.value)} required />
      </div>
      <div className="fieldRow">
        <input type="date" value={form.dateOfBirth} onChange={(event) => update("dateOfBirth", event.target.value)} required />
        <select value={form.gender} onChange={(event) => update("gender", event.target.value)}>
          <option value="">Gender</option>
          <option value="Female">Female</option>
          <option value="Male">Male</option>
          <option value="Non-binary">Non-binary</option>
          <option value="Prefer not to say">Prefer not to say</option>
        </select>
      </div>
      <input type="email" placeholder="Email" value={form.email} onChange={(event) => update("email", event.target.value)} />
      <input placeholder="Phone" value={form.phoneNumber} onChange={(event) => update("phoneNumber", event.target.value)} />
      <input placeholder="Address" value={form.address} onChange={(event) => update("address", event.target.value)} />
      <button disabled={busy}>Create patient</button>
    </form>
  );
}

function PatientsPanel({ patients, selectedPatientId, onSelect }) {
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

function PatientWorkspace({
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

function ScansTab({ scans, selectedScan, onSelectScan, patient }) {
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

function UploadTab({ patient, scanFile, setScanFile, onSubmit, busy }) {
  return (
    <form className="uploadTab" onSubmit={onSubmit}>
      <div className="uploadCopy">
        <span className="eyebrow">Upload for</span>
        <h3>{patient.firstName} {patient.lastName}</h3>
        <p>The scan will be saved directly to this patient's history after classification.</p>
      </div>

      <label className="fileDrop">
        <FileImage size={30} />
        <strong>{scanFile ? scanFile.name : "Choose MRI image"}</strong>
        <span>JPEG, PNG, BMP, or WEBP up to 10 MB</span>
        <input type="file" accept="image/png,image/jpeg,image/bmp,image/webp" onChange={(event) => setScanFile(event.target.files?.[0] ?? null)} />
      </label>

      <button className="iconButton uploadCta" disabled={busy || !scanFile}>
        <Upload size={18} />
        Classify scan for {patient.firstName}
      </button>
    </form>
  );
}

function DetailsTab({ patient }) {
  return (
    <div className="detailsGrid">
      <Info icon={<CalendarDays size={17} />} label="Date of birth" value={patient.dateOfBirth} />
      <Info icon={<UserRound size={17} />} label="Gender" value={patient.gender || "Unknown"} />
      <Info icon={<Mail size={17} />} label="Email" value={patient.email || "No email"} />
      <Info icon={<Phone size={17} />} label="Phone" value={patient.phoneNumber || "No phone"} />
      <Info icon={<MapPin size={17} />} label="Address" value={patient.address || "No address"} wide />
    </div>
  );
}

function Info({ icon, label, value, wide }) {
  return (
    <div className={`info ${wide ? "wide" : ""}`}>
      {icon}
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function ScanDetail({ scan, patient }) {
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

function ProbabilityList({ probabilities }) {
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

async function api(path, options = {}) {
  const response = await fetch(path, options);

  if (options.empty && response.ok) {
    return null;
  }

  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("application/json") ? await response.json() : await response.text();

  if (!response.ok) {
    throw new Error(body?.error || body || `Request failed with ${response.status}`);
  }

  return body;
}

createRoot(document.getElementById("root")).render(<App />);
