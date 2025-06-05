#include <nn/album/album_SaveScreenshot.h>

extern "C"
{
    const nn::album::AlbumReportOption ReportOption = nn::album::AlbumReportOption_ReportAlways;
    void Switch_SaveScreenShot()
    {
        nn::album::Initialize();

        nn::album::SaveCurrentScreenshot(ReportOption);

        nn::album::Finalize();
    }
}
