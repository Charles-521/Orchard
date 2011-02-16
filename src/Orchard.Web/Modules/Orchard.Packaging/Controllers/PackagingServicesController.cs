using System;
using System.IO;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using NuGet;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.AppData;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Packaging.Services;
using Orchard.Security;
using Orchard.Themes;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using Orchard.Utility.Extensions;
using IPackageManager = Orchard.Packaging.Services.IPackageManager;
using PackageBuilder = Orchard.Packaging.Services.PackageBuilder;

namespace Orchard.Packaging.Controllers {
    [OrchardFeature("PackagingServices")]
    [Themed, Admin]
    public class PackagingServicesController : Controller {

        private readonly ShellSettings _shellSettings;
        private readonly IPackageManager _packageManager;
        private readonly IAppDataFolderRoot _appDataFolderRoot;
        private readonly INotifier _notifier;

        public PackagingServicesController(
            ShellSettings shellSettings,
            IPackageManager packageManager,
            INotifier notifier,
            IAppDataFolderRoot appDataFolderRoot,
            IOrchardServices services) {

            _shellSettings = shellSettings;
            _packageManager = packageManager;
            _notifier = notifier;
            _appDataFolderRoot = appDataFolderRoot;
            Services = services;

            T = NullLocalizer.Instance;
            Logger = Logging.NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public IOrchardServices Services { get; set; }
        public Logging.ILogger Logger { get; set; }

        public ActionResult AddTheme(string returnUrl) {
            if (_shellSettings.Name != ShellSettings.DefaultName || !Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to add themes")))
                return new HttpUnauthorizedResult();

            return View();
        }

        [HttpPost, ActionName("AddTheme")]
        public ActionResult AddThemePOST(string returnUrl) {
            if (_shellSettings.Name != ShellSettings.DefaultName || !Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to add themes")))
                return new HttpUnauthorizedResult();

            return InstallPackage(returnUrl, Request.RawUrl);
        }

        [HttpPost, ActionName("RemoveTheme")]
        public ActionResult RemoveThemePOST(string themeId, string returnUrl, string retryUrl) {
            if (_shellSettings.Name != ShellSettings.DefaultName || !Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to remove themes")))
                return new HttpUnauthorizedResult();

            return UninstallPackage(PackageBuilder.BuildPackageId(themeId, DefaultExtensionTypes.Theme), returnUrl, retryUrl);
        }

        public ActionResult AddModule(string returnUrl) {
            if (_shellSettings.Name != ShellSettings.DefaultName || !Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to add modules")))
                return new HttpUnauthorizedResult();

            return View();
        }

        [HttpPost, ActionName("AddModule")]
        public ActionResult AddModulePOST(string returnUrl) {
            if (_shellSettings.Name != ShellSettings.DefaultName || !Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to add modules")))
                return new HttpUnauthorizedResult();

            return InstallPackage(returnUrl, Request.RawUrl);
        }

        public ActionResult InstallPackage(string returnUrl, string retryUrl) {
            if (_shellSettings.Name != ShellSettings.DefaultName || !Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to install packages")))
                return new HttpUnauthorizedResult();

            try {
                if (Request.Files != null &&
                    Request.Files.Count > 0 &&
                    !string.IsNullOrWhiteSpace(Request.Files[0].FileName)) {
                    ModelState.AddModelError("File", T("Select a file to upload.").ToString());
                }

                foreach (string fileName in Request.Files) {
                    HttpPostedFileBase file = Request.Files[fileName];
                    if (file != null) {
                        string fullFileName = Path.Combine(_appDataFolderRoot.RootFolder, fileName + ".nupkg").Replace(Path.DirectorySeparatorChar, '/');
                        file.SaveAs(fullFileName);
                        PackageInfo info = _packageManager.Install(new ZipPackage(fullFileName), _appDataFolderRoot.RootFolder, HostingEnvironment.MapPath("~/"));
                        System.IO.File.Delete(fullFileName);
                    }
                }

                return this.RedirectLocal(returnUrl, "~/");
            } catch (Exception exception) {
                this.Error(exception, T("Package uploading and installation failed."), Logger, Services.Notifier);

                return Redirect(retryUrl);
            }
        }

        public ActionResult UninstallPackage(string id, string returnUrl, string retryUrl) {
            if (_shellSettings.Name != ShellSettings.DefaultName || !Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to uninstall packages")))
                return new HttpUnauthorizedResult();

            try {
                _packageManager.Uninstall(id, HostingEnvironment.MapPath("~/"));

                _notifier.Information(T("Uninstalled package \"{0}\"", id));

                return this.RedirectLocal(returnUrl, "~/");
            } catch (Exception exception) {
                this.Error(exception, T("Uninstall failed: {0}", exception.Message), Logger, Services.Notifier);

                return Redirect(retryUrl);
            }
        }
    }
}