Changes made (summary)
•	Added a new constant Constants.RyzenPowerPlan for the additional 1usmus Ryzen GUID and validated it in Constants static constructor.
•	Extended PowerPlanDetector.GetOptimalHighPerformancePlan() to consider both known Ryzen GUIDs (preferred) then fall back to UltimatePerformance.
•	Modified Form1 startup logic to:
•	Determine the preferred high-performance plan (Ryzen variants → HighPerformance as fallback).
•	Unconditionally attempt to enforce it on startup via powercfg /setactive <GUID> if it differs from the current active plan.
•	Create IdleChecker using the chosen plan so the app will restore that plan after idle.
•	Added logging of actions and any errors.
•	Updated PowerPlanDetectorTests to accept the additional Ryzen GUID and validate the new constant.
Notes and rationale
•	Implemented exactly as you requested: unconditional enforcement and use of powercfg /setactive.
•	The new Ryzen GUID is treated as an acceptable high-performance plan for AMD Ryzen users; the detection prefers the original RyzenUniversal GUID first, then the new Ryzen GUID, then UltimatePerformance.
•	Tests were updated so they continue to reflect intended behavior (build and tests passed locally in the workspace).