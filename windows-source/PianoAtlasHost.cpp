#include <windows.h>

#include <string>

namespace
{
    using hostfxr_handle = void*;
    using hostfxr_initialize_for_runtime_config_fn = int(__cdecl*)(const wchar_t*, const void*, hostfxr_handle*);
    using hostfxr_get_runtime_delegate_fn = int(__cdecl*)(hostfxr_handle, int, void**);
    using hostfxr_close_fn = int(__cdecl*)(hostfxr_handle);
    using load_assembly_and_get_function_pointer_fn = int(__cdecl*)(const wchar_t*, const wchar_t*, const wchar_t*, const wchar_t*, void*, void**);
    using component_entry_point_fn = int(__stdcall*)(void*, int);

    constexpr int hdt_load_assembly_and_get_function_pointer = 5;

    std::wstring DirectoryOfExe()
    {
        wchar_t buffer[MAX_PATH];
        DWORD length = GetModuleFileNameW(nullptr, buffer, MAX_PATH);
        std::wstring path(buffer, length);
        size_t slash = path.find_last_of(L"\\/");
        return slash == std::wstring::npos ? L"." : path.substr(0, slash);
    }

    std::wstring Join(const std::wstring& left, const std::wstring& right)
    {
        if (left.empty()) return right;
        wchar_t last = left[left.size() - 1];
        if (last == L'\\' || last == L'/') return left + right;
        return left + L"\\" + right;
    }

    void ShowError(const std::wstring& message)
    {
        MessageBoxW(nullptr, message.c_str(), L"Piano Atlas", MB_OK | MB_ICONERROR);
    }

    void EnableHighDpi()
    {
        HMODULE user32 = GetModuleHandleW(L"user32.dll");
        if (!user32) return;

        using set_process_dpi_awareness_context_fn = BOOL(WINAPI*)(HANDLE);
        auto setDpiAwarenessContext = reinterpret_cast<set_process_dpi_awareness_context_fn>(
            GetProcAddress(user32, "SetProcessDpiAwarenessContext"));

        if (setDpiAwarenessContext)
        {
            setDpiAwarenessContext(reinterpret_cast<HANDLE>(-4)); // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
        }
    }
}

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR, int)
{
    EnableHighDpi();
    HRESULT apartment = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);

    std::wstring baseDir = DirectoryOfExe();
    std::wstring runtimeRoot = Join(baseDir, L"runtime");
    std::wstring hostfxrPath = Join(runtimeRoot, L"host\\fxr\\6.0.36\\hostfxr.dll");
    std::wstring runtimeConfig = Join(baseDir, L"PianoAtlasLauncher.runtimeconfig.json");
    std::wstring launcherAssembly = Join(baseDir, L"PianoAtlasLauncher.dll");

    SetEnvironmentVariableW(L"PIANO_ATLAS_BASE_DIR", baseDir.c_str());
    SetEnvironmentVariableW(L"DOTNET_ROOT", runtimeRoot.c_str());

    HMODULE hostfxr = LoadLibraryW(hostfxrPath.c_str());
    if (!hostfxr)
    {
        ShowError(L"Piano Atlas could not find its bundled Windows runtime files.");
        if (SUCCEEDED(apartment)) CoUninitialize();
        return 10;
    }

    auto init = reinterpret_cast<hostfxr_initialize_for_runtime_config_fn>(
        GetProcAddress(hostfxr, "hostfxr_initialize_for_runtime_config"));
    auto getDelegate = reinterpret_cast<hostfxr_get_runtime_delegate_fn>(
        GetProcAddress(hostfxr, "hostfxr_get_runtime_delegate"));
    auto close = reinterpret_cast<hostfxr_close_fn>(
        GetProcAddress(hostfxr, "hostfxr_close"));

    if (!init || !getDelegate || !close)
    {
        ShowError(L"Piano Atlas could not initialize its Windows runtime.");
        if (SUCCEEDED(apartment)) CoUninitialize();
        return 11;
    }

    hostfxr_handle context = nullptr;
    int rc = init(runtimeConfig.c_str(), nullptr, &context);
    if (rc != 0 || !context)
    {
        ShowError(L"Piano Atlas could not start its Windows runtime.");
        if (SUCCEEDED(apartment)) CoUninitialize();
        return 12;
    }

    void* loadAssemblyRaw = nullptr;
    rc = getDelegate(context, hdt_load_assembly_and_get_function_pointer, &loadAssemblyRaw);
    close(context);

    if (rc != 0 || !loadAssemblyRaw)
    {
        ShowError(L"Piano Atlas could not load its app engine.");
        if (SUCCEEDED(apartment)) CoUninitialize();
        return 13;
    }

    auto loadAssembly = reinterpret_cast<load_assembly_and_get_function_pointer_fn>(loadAssemblyRaw);
    void* entryRaw = nullptr;
    rc = loadAssembly(
        launcherAssembly.c_str(),
        L"PianoAtlasRelease.PianoAtlasComponent, PianoAtlasLauncher",
        L"Run",
        nullptr,
        nullptr,
        &entryRaw);

    if (rc != 0 || !entryRaw)
    {
        ShowError(L"Piano Atlas could not open the app window.");
        if (SUCCEEDED(apartment)) CoUninitialize();
        return 14;
    }

    auto entry = reinterpret_cast<component_entry_point_fn>(entryRaw);
    wchar_t* commandLine = GetCommandLineW();
    int sizeBytes = static_cast<int>((wcslen(commandLine) + 1) * sizeof(wchar_t));
    int result = entry(commandLine, sizeBytes);
    if (SUCCEEDED(apartment)) CoUninitialize();
    return result;
}
