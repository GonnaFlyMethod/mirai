# Mirai

Mirai is a patient-centered brain MRI classification app. It combines an ASP.NET Core API, SQLite storage, an ONNX brain tumor classifier, and a React/Vite frontend.

The app lets you:

- Create and search patients.
- Open a patient workspace with details, scan history, and upload actions.
- Upload an MRI scan for the selected patient.
- Classify the scan using the local ONNX model.
- Review the predicted tumor class, confidence score, probability breakdown, and scan image.

## Screenshots

### Patient scan history and classification result

![Patient scan history and classification result](media/Screenshot.png)

### Upload a new MRI scan for the selected patient

![Upload a new MRI scan](media/Screenshot1.png)

### Patient details tab

![Patient details](media/Screenshot2.png)

## Tech Stack

- Backend: ASP.NET Core 8
- Database: SQLite
- Machine learning inference: ONNX Runtime
- Image preprocessing: SixLabors ImageSharp
- Frontend: React 19, Vite, lucide-react
- Model: ResNet50-based brain tumor classifier exported to ONNX

## Project Structure

```text
path/to/MedScans/
  backend/
    Api/                       API endpoints
    Infrastructure/            EF Core DbContext and persistence helpers
    Patients/                  Patient entity, repository, service
    Scans/                     Scan entity, repository, service, ONNX analyzer
    Models/
      brain-tumor-resnet50.onnx
      brain-tumor-resnet50.pth
      brain-tumor-resnet50.metadata.json
    appsettings.json           API configuration
    Program.cs                 ASP.NET Core startup
    MedScans.csproj            Backend project file
    app.db                     SQLite database
  tests/
    MedScans.Tests/            Backend unit tests
  frontend/                    React/Vite frontend
  ModelTraining/               Python training/export scripts
  datasets/
    brain-mri/                 Local training dataset
  media/                       README screenshots
  MedScans.sln                 Backend solution and tests
```

## Prerequisites

Install these tools before running the project:

- .NET SDK 8.0 or newer
- Node.js 20 or newer
- npm, included with Node.js

Check versions:

```powershell
dotnet --version
node --version
npm --version
```

## Install Dependencies

From the project root:

```powershell
dotnet restore MedScans.sln
```

Install frontend dependencies:

```powershell
cd frontend
npm install
cd ..
```

The backend uses these main NuGet packages:

- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.ML.OnnxRuntime`
- `SixLabors.ImageSharp`

The frontend uses:

- `react`
- `react-dom`
- `vite`
- `@vitejs/plugin-react`
- `lucide-react`

## Model Files

The API expects the ONNX model at:

```text
backend/Models/brain-tumor-resnet50.onnx
```

If the ONNX file is missing, scan classification will fail. Keep the model file in `backend/Models/` or update `backend/appsettings.json` to point to the correct location.

## Training Dataset

Use the Kaggle **Brain Tumor MRI Dataset** by Masoud Nickparvar:

```text
https://www.kaggle.com/datasets/masoudnickparvar/brain-tumor-mri-dataset/data
```

After downloading and extracting it, place the dataset in this local folder:

```text
datasets/brain-mri/
  Training/
    glioma/
    meningioma/
    notumor/
    pituitary/
  Testing/
    glioma/
    meningioma/
    notumor/
    pituitary/
```

Each class folder should contain MRI image files for that class.

Train and export the model from the project root:

```powershell
python ModelTraining/train_resnet50.py --epochs 10 --batch-size 4
```

By default, the script reads `datasets/brain-mri` and writes the generated `.pth`, `.onnx`, and metadata files to `backend/Models`.

The smaller batch size is a safer default for Windows CPU training. If you train on a machine with more memory or a CUDA GPU, you can try a larger value such as `--batch-size 16` or `--batch-size 32`.

## Database

The project uses SQLite:

```text
backend/app.db
```

The connection string is configured in `backend/appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Data Source=app.db"
}
```

Run the API from the `backend/` directory so `Data Source=app.db` points to `backend/app.db`.

The API calls `EnsureCreatedAsync()` on startup, so if `backend/app.db` does not exist, SQLite tables are created automatically.

Main tables:

- `Patients`
- `BrainScans`

Each scan belongs to a patient through:

```text
BrainScans.PatientId -> Patients.Id
```

The UI uploads scans only inside a selected patient workspace, so new scans are linked to that patient.

## Run the Project

You need two terminals: one for the API and one for the frontend.

### Terminal 1: Start the API

From the backend directory:

```powershell
cd backend
dotnet run --urls http://localhost:5091
```

Health check:

```text
http://localhost:5091/api/health
```

Expected response:

```json
{
  "status": "ok",
  "timestamp": "..."
}
```

### Terminal 2: Start the Frontend

```powershell
cd frontend
npm run dev
```

Open:

```text
http://127.0.0.1:5173
```

The Vite dev server proxies `/api` requests to:

```text
http://localhost:5091
```

This is configured in:

```text
frontend/vite.config.js
```

## Build for Production

Build the backend (from the project root):

```powershell
dotnet build MedScans.sln
```

Run backend tests (from the root path):

```powershell
dotnet test MedScans.sln
```

Build the frontend:

```powershell
cd frontend
npm run build
```

The frontend production output is written to:

```text
frontend/dist/
```

## Run with Docker

The project includes a production Docker setup:

- `Dockerfile` builds the React frontend and publishes the ASP.NET Core API.
- The frontend `dist/` files are copied into `wwwroot`.
- ASP.NET Core serves both the API and frontend from one container.
- `docker-compose.yml` mounts the local SQLite database and model files.

Prerequisites:

- Docker Desktop

Build and run with Docker Compose (from the root):

```powershell
docker compose up --build
```

Open:

```text
http://localhost:8080
```

API health check:

```text
http://localhost:8080/api/health
```

Stop the container:

```powershell
docker compose down
```

Build only:

```powershell
docker build -t medscans .
```

## How to Use the App

1. Start the API and frontend.
2. Open `http://127.0.0.1:5173`.
3. Select a patient from the left sidebar.
4. Use the patient workspace tabs:
   - `Scans`: view that patient's scan history and classification results.
   - `Upload new scan`: upload and classify a new MRI for the selected patient.
   - `Patient details`: view demographics and contact information.
5. In the `Upload new scan` tab, choose an MRI image.
6. Click `Classify scan for <patient>`.
7. After classification, the scan appears in the selected patient's `Scans` tab.

Supported upload formats:

- JPEG
- PNG
- BMP
- WEBP

Maximum upload size:

```text
10 MB
```

## API Endpoints

Health:

```http
GET /api/health
```

Patients:

```http
GET    /api/patients
GET    /api/patients/{id}
POST   /api/patients
DELETE /api/patients/{id}
```

Scans:

```http
GET  /api/scans
GET  /api/scans/{id}
GET  /api/scans/{id}/image
POST /api/scans/analyze
```

`POST /api/scans/analyze` expects multipart form data:

```text
image     MRI image file
patientId selected patient id
```

## License

Add project license information here if this repository is distributed publicly.
