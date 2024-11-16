# AiLogAnalyzer

## Overview
**AiLogAnalyzer** is a WinUI3 application designed for developers and system administrators to analyze logs efficiently. It leverages AI models (OpenAI and Ollama) to provide insights and streamline log analysis.

The application monitors hotkeys, retrieves clipboard content, and interacts with AI models for processing. It's modern, flexible, and tailored for high productivity.

---

## Features
- **Log Analysis with AI**: Interact with AI models like OpenAI or Ollama for log processing.
- **Clipboard Monitoring**: Automatically fetches clipboard content when triggered.
- **Customizable**: Easily configurable settings and flexible architecture.

---

## Requirements
Before you begin, ensure you have the following installed:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Windows 10 version 1809 (10.0.17763.0) or later
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/) with the **WinUI3** and **.NET Desktop Development** workloads

---

## Setup Instructions

### Clone the Repository
```bash
git clone https://github.com/smartzan63/AiLogAnalyzer.git
cd AiLogAnalyzer
```

### Install Required Tools
This project uses `reportgenerator` to generate test coverage reports.

1. Install `reportgenerator` globally:
   ```bash
   dotnet tool install --global dotnet-reportgenerator-globaltool
   ```
2. Ensure the global tools path is in your system's `PATH` variable:
    - **Windows**: Typically `C:\Users\<YourUsername>\.dotnet\tools`
    - Add it to your environment variables if it's missing.

3. Alternatively, install it locally for the project:
   ```bash
   dotnet new tool-manifest
   dotnet tool install dotnet-reportgenerator-globaltool
   ```
   Use the command `dotnet tool run reportgenerator` in scripts when running locally.

---

## Running Tests and Generating Reports

1. **Run Tests with Coverage**:
   Use the PowerShell script to clean, test, and generate coverage reports.
   ```bash
   .\GenerateCoverageReport.ps1
   ```

2. **View HTML Report**:
   The HTML report is generated in the `Code\coverage-report` directory. The script will open the report in your default browser automatically.

---

## Contribution
Feel free to contribute to this project by opening issues or submitting pull requests.

1. Fork the repository.
2. Create a new branch (`git checkout -b feature/YourFeature`).
3. Commit your changes (`git commit -m "Add Your Feature"`).
4. Push the branch (`git push origin feature/YourFeature`).
5. Open a Pull Request.

---

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Contact
For any inquiries or support, feel free to reach out:
- **Email**: [smartzan63@gmail.com](mailto:smartzan63@gmail.com])
- **GitHub**: [smartzan63](https://github.com/smartzan63)

