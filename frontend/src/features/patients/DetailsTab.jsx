import { CalendarDays, Mail, MapPin, Phone, UserRound } from "lucide-react";

export default function DetailsTab({ patient }) {
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
