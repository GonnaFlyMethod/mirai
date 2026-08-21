import { CalendarClock, FileText, Search, Upload } from "lucide-react";

export default function HistoryTab({
  patient,
  historyRecords,
  historyFile,
  setHistoryFile,
  historySearch,
  setHistorySearch,
  onUpload,
  onSearch,
  busy
}) {
  return (
    <div className="historyTab">
      <form className="historyUpload" onSubmit={onUpload}>
        <label className="fileDrop">
          <FileText size={26} />
          <strong>{historyFile ? historyFile.name : "Choose scanned history (PDF)"}</strong>
          <span>PDF up to 20 MB</span>
          <input
            type="file"
            accept="application/pdf"
            onChange={(event) => setHistoryFile(event.target.files?.[0] ?? null)}
          />
        </label>
        <button className="iconButton uploadCta" disabled={busy || !historyFile}>
          <Upload size={18} />
          Upload history for {patient.firstName}
        </button>
      </form>

      <form className="historySearch" onSubmit={onSearch}>
        <label className="searchField">
          <CalendarClock size={16} />
          <input
            type="datetime-local"
            value={historySearch.datetime}
            onChange={(event) => setHistorySearch({ ...historySearch, datetime: event.target.value })}
          />
        </label>
        <label className="searchField">
          <input
            placeholder="Title"
            value={historySearch.title}
            onChange={(event) => setHistorySearch({ ...historySearch, title: event.target.value })}
          />
        </label>
        <label className="searchField">
          <input
            placeholder="Description"
            value={historySearch.description}
            onChange={(event) => setHistorySearch({ ...historySearch, description: event.target.value })}
          />
        </label>
        <button className="secondary iconButton" type="submit">
          <Search size={16} />
          Search
        </button>
      </form>

      {historyRecords.length === 0 ? (
        <div className="emptyPanel">
          <FileText size={42} />
          <h3>No history records</h3>
          <p>Upload a scanned patient history PDF to get started.</p>
        </div>
      ) : (
        <ul className="historyList">
          {historyRecords.map((record) => (
            <li className="historyRecord" key={record.id}>
              <span className="historyDate">{new Date(record.datetime).toLocaleString()}</span>
              <strong>{record.title}</strong>
              <p>{record.description}</p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
