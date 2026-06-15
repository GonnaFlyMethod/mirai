import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  Brain,
  CircleAlert,
  FileImage,
  Plus,
  RefreshCw,
  Search,
  Upload,
  UserRound
} from "lucide-react";
import { api } from "./api/client";
import Metric from "./components/Metric";
import { emptyPatient } from "./constants/patients";
import PatientForm from "./features/patients/PatientForm";
import PatientsPanel from "./features/patients/PatientsPanel";
import PatientWorkspace from "./features/patients/PatientWorkspace";

export default function App() {
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
