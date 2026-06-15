import { FileImage, Upload } from "lucide-react";

export default function UploadTab({ patient, scanFile, setScanFile, onSubmit, busy }) {
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
