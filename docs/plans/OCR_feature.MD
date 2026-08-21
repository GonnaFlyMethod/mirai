# Plan: Recognition of history of patients


Description for the feature:
we need to add posibility for doctors to upload handwritten histories of patients as .pdf (pages will be scanned). the flow starts on frontend,
doctor clicks button "Uplaod patient's history, the button is linked to the profile of an individual patient. when the button is pressed, and
file with the history is uploaded the request goes to the server.

Server implements following endpoints:
- POST /patients/{id}/histories - bounds a history to individual patient with id {id}
- GET /patients/{id}/histories - retrievis history for individual patient with id {id}.

history of a patient is a journal, one record in that journal consists of datetime of the record (format: ISO8601), title of record and description of the record, eg:

2026-03-12 Heartattack Patient got heartattack when he took penicilin...

2026-03-12 - datetime (it might ommit HH:SS:MM), the format is ISO8601
Heartattack - title
Patient got heartattack when he took penicilin... - description

also there query params for the histories of patients (get request)
- GET /patients/{id}/histories?title=<title of record>
- GET /patients/{id}/histories?datetime=<datetime of record>
- GET /patients/{id}/histories?description=<description of record>

search queries can be combined in different ways:
- GET /patients/{id}/histories?title=<title of record>&datetime=<datetime of record>
- GET /patients/{id}/histories?title=<title of record>&datetime=<datetime of record>&description=<description of record>


this query params allow doctors to search for the records based on datetime of record and/or title and/or description of a record. The text for title
and description should use smart search based on semantic similarities. Let's use KNN or embedding-like search in (please, propose your solution 
and give me a chance to review it). The search might yield multiple records of a journal that match the search.

Database:
create a new table Histories, the table should contain following columns: 
id (uuid) REQUIRED
patient_id (uuid) REQUIRED -> foreign key to the column id in the table Patients
datetime (datetime) REQUIRED
title (VARCHAR(1500)) REQUIRED
description (TEXT) REQUIRED

the names of the columns should match the existing case in other tables.

### Task 1: Add support on backend
- [ ] Incorporate baidu's Unlimited OCR(https://github.com/baidu/Unlimited-OCR) to recognize the text from pdf or images on pdf, I've only seen python's implementation, so i think
bindings are inavitable, if you will find C# version - let's grab this, otherwise let's proceed with bindings.
- [ ] Use AppDbContext.cs to properly register the new table - Histories
- [ ] Extend existing interface of patient's repository  - IPatientRepository
- [ ] Extend existing repository of Patients - PatientRepository (creation, retrieval of history by patient id and search of histories). 
Methods names and definition: 
CreateHistory(<all paramrs required to create history>), 
GetHistory(<patient_id>), 
List<HistoryRecord> SearchInHistory(<search params described above: datetime, title, description>)
- [ ] Extend existing service of Patients - PatientService (creation, retrieval of history by id and search of histories). 
Methods names and definition: 
void CreateHistory(<pdf_file>, <all paramrs required to create history>), 
List<HistoryRecord> GetHistory(<patient_id>),
List<HistoryRecord> SearchInHistory(<search params described above: datetime, title, description>)

private method:
	string Recognize(<pdf file>) accepts pdf file and returns recognized pdf text, formulas, text from images and so on as string

- [ ] CreateHistory method of a service, it should iterate over recognized text from string Recognize(<pdf file>), split it onto chunks: one chunk = one record in jorunal
consists from <record's datetime>, <record's title>, <record's description>

### Task 2
- [ ] Links methods of service to appropriate endpoinds on the server:
POST /patients/{id}/histories -> CreateHistory method in the service
GET /patients/{id}/histories without query params -> List<HistoryRecord> GetHistory(<patient_id>)
GET /patients/{id}/histories with query params like datetime and/or title and/or description -> List<HistoryRecord> SearchInHistory

### Task 3: Add support of the functionality on frontend
- [ ] Add the new client code to the frontend that will utilize the above mentioned endpoints. Client should make POST request to
POST /patients/{id}/histories when doctor uploaded .pdf scan and hit the button.
- [ ] Add input fields for search through the historie's records: datetime search - calendar widget, title - input field, 
description - input field
- [ ] Add one more tab in the patient's webpage alongside with scans and patient's info - history, the search fields should be added into this
tab and, automatically when user navigates to this tab, the frontend should execute request to just get all history of the patient - GET /patients/{id}/histories