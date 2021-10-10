// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "SpinLock.h"

struct SpinLock {
private:
	const int SPIN_ITERATIONS = 30;
	volatile LONG lock;
public:
	SpinLock() { lock = 0; }

	void GetLock()
	{
		int iterations = 0;
		while (InterlockedCompareExchange(&lock, 1, 0) == 0) {
			YieldProcessor();
			if (iterations++ > SPIN_ITERATIONS) {
				iterations = 0;

				Sleep(0);
			}
		}
	}

	void ReleaseLock() { lock = 0; }
};

struct GWPSVoltageCurrent {
	volatile double Voltage;
	volatile double Current;

	volatile bool SettingsAccessed;
};

struct MemStructure {
	SpinLock spinlock;

	volatile double CurrentTemperature;
	volatile double SetPoint;
	volatile bool SetPointAccessed;

	GWPSVoltageCurrent Channel1;
	GWPSVoltageCurrent Channel2;
};

#define SHMEMSIZE sizeof(MemStructure) 


static LPVOID lpvMem = NULL;      // pointer to shared memory
static HANDLE hMapObject = NULL;  // handle to file mapping

// The DLL entry-point function sets up shared memory using a 
// named file-mapping object. 

BOOL WINAPI DllMain(HINSTANCE hinstDLL,  // DLL module handle
	DWORD fdwReason,              // reason called 
	LPVOID lpvReserved)           // reserved 
{
	BOOL fInit, fIgnore;

	switch (fdwReason) {
		// DLL load due to process initialization or LoadLibrary

	case DLL_PROCESS_ATTACH:

		// Create a named file mapping object

		hMapObject = CreateFileMapping(
			INVALID_HANDLE_VALUE,      // use paging file
			NULL,                      // default security attributes
			PAGE_READWRITE,            // read/write access
			0,                         // size: high 32-bits
			SHMEMSIZE,                 // size: low 32-bits
			TEXT("TECPIDmemfilemap")); // name of map object

		if (hMapObject == NULL)
			return FALSE;

		// The first process to attach initializes memory

		fInit = (GetLastError() != ERROR_ALREADY_EXISTS);

		// Get a pointer to the file-mapped shared memory

		lpvMem = MapViewOfFile(
			hMapObject,     // object to map view of
			FILE_MAP_WRITE, // read/write access
			0,              // high offset:  map from
			0,              // low offset:   beginning
			0);             // default: map entire file
		if (lpvMem == NULL)
			return FALSE;

		// Initialize memory if this is the first process

		if (fInit)
			memset(lpvMem, '\0', SHMEMSIZE);

		break;

		// The attached process creates a new thread

	case DLL_THREAD_ATTACH:
		break;

		// The thread of the attached process terminates

	case DLL_THREAD_DETACH:
		break;

		// DLL unload due to process termination or FreeLibrary

	case DLL_PROCESS_DETACH:

		// Unmap shared memory from the process's address space

		fIgnore = UnmapViewOfFile(lpvMem);

		// Close the process's handle to the file-mapping object

		fIgnore = CloseHandle(hMapObject);

		break;

	default:
		break;
	}

	return TRUE;
}

extern "C" {

	__declspec(dllexport) double _cdecl GetTemperature()
	{
		double temp;

		MemStructure* pSstr = (MemStructure*)lpvMem;
		pSstr->spinlock.GetLock();

		temp = pSstr->CurrentTemperature;

		pSstr->spinlock.ReleaseLock();

		return temp;
	}
	__declspec(dllexport) double _cdecl GetSetPoint()
	{
		double sp;

		MemStructure* pSstr = (MemStructure*)lpvMem;
		pSstr->spinlock.GetLock();

		sp = pSstr->SetPoint;

		pSstr->spinlock.ReleaseLock();

		return sp;
	}

	__declspec(dllexport) double _cdecl GetSPOPC()
	{
		double sp;
		MemStructure* pSstr = (MemStructure*)lpvMem;
		pSstr->spinlock.GetLock();

		if (!pSstr->SetPointAccessed) {
			sp = pSstr->SetPoint;
			pSstr->SetPointAccessed = true;
		} else {
			sp = NAN;
		}

		pSstr->spinlock.ReleaseLock();

		return sp;
	}

	__declspec(dllexport) void _cdecl SetTemperature(double temp)
	{
		MemStructure* pSstr = (MemStructure*)lpvMem;
		pSstr->spinlock.GetLock();

		pSstr->CurrentTemperature = temp;

		pSstr->spinlock.ReleaseLock();
	}
	__declspec(dllexport) void _cdecl SetSetPoint(double setpoint)
	{
		MemStructure* pSstr = (MemStructure*)lpvMem;
		pSstr->spinlock.GetLock();

		pSstr->SetPoint = setpoint;
		pSstr->SetPointAccessed = false;

		pSstr->spinlock.ReleaseLock();
	}

	__declspec(dllexport) void _cdecl SetOutVI(int channel, double voltage, double current)
	{
		MemStructure* pSstr = (MemStructure*)lpvMem;
		pSstr->spinlock.GetLock();

		if (channel == 1) {
			pSstr->Channel1.Voltage = voltage;
			pSstr->Channel1.Current = current;
			pSstr->Channel1.SettingsAccessed = false;
		} else if (channel == 2) {
			pSstr->Channel2.Voltage = voltage;
			pSstr->Channel2.Current = current;
			pSstr->Channel2.SettingsAccessed = false;
		}

		pSstr->spinlock.ReleaseLock();
	}
	__declspec(dllexport) void _cdecl GetVIOnce(int channel, double& voltage, double& current)
	{
		MemStructure* pSstr = (MemStructure*)lpvMem;

		pSstr->spinlock.GetLock();

		if (channel == 1) {
			if (pSstr->Channel1.SettingsAccessed) {
				voltage = NAN;
				current = NAN;
			} else {
				voltage = pSstr->Channel1.Voltage;
				current = pSstr->Channel1.Current;
				pSstr->Channel1.SettingsAccessed = true;
			}
		} else if (channel == 2) {
			if (pSstr->Channel2.SettingsAccessed) {
				voltage = NAN;
				current = NAN;
			} else {
				voltage = pSstr->Channel2.Voltage;
				current = pSstr->Channel2.Current;
				pSstr->Channel2.SettingsAccessed = true;
			}
		} else {
			voltage = NAN;
			current = NAN;
		}

		pSstr->spinlock.ReleaseLock();
	}

	__declspec(dllexport) void _cdecl GetVI(int channel, double& voltage, double& current)
	{
		MemStructure* pSstr = (MemStructure*)lpvMem;

		pSstr->spinlock.GetLock();

		if (channel == 1) {
				voltage = pSstr->Channel1.Voltage;
				current = pSstr->Channel1.Current;
		} else if (channel == 2) {
				voltage = pSstr->Channel2.Voltage;
				current = pSstr->Channel2.Current;
		} else {
			voltage = NAN;
			current = NAN;
		}

		pSstr->spinlock.ReleaseLock();
	}


}


