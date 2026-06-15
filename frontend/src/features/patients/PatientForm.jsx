export default function PatientForm({ busy, form, setForm, onSubmit }) {
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
