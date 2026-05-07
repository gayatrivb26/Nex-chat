const { app, BrowserWindow, Menu, Tray, ipcMain, dialog, nativeImage, protocol } = require('electron');
const { autoUpdater } = require('electron-updater');
const path = require('node:path');

let mainWindow;
let tray;
let isQuitting = false;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 820,
    minWidth: 900,
    minHeight: 600,
    frame: false,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      webSecurity: true,
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  if (process.env.ELECTRON_START_URL) {
    mainWindow.loadURL(process.env.ELECTRON_START_URL);
  } else {
    mainWindow.loadFile(path.join(__dirname, '../dist/nexchat/browser/index.html'));
  }

  mainWindow.on('close', (event) => {
    if (!isQuitting) {
      event.preventDefault();
      mainWindow.hide();
    }
  });
}

function createTray() {
  const icon = nativeImage.createEmpty();
  tray = new Tray(icon);
  tray.setToolTip('NexChat');
  tray.setContextMenu(Menu.buildFromTemplate([
    { label: 'Open', click: () => mainWindow?.show() },
    { label: 'Mute Notifications', type: 'checkbox' },
    { type: 'separator' },
    { label: 'Quit', click: () => { isQuitting = true; app.quit(); } }
  ]));
  tray.on('double-click', () => mainWindow?.show());
}

app.whenReady().then(() => {
  protocol.registerHttpProtocol('chat', (request) => {
    mainWindow?.show();
    mainWindow?.webContents.send('deeplink:url', request.url);
  });

  createWindow();
  createTray();
  autoUpdater.checkForUpdatesAndNotify();
  setInterval(() => autoUpdater.checkForUpdatesAndNotify(), 4 * 60 * 60 * 1000);
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

ipcMain.handle('app:get-version', () => app.getVersion());
ipcMain.handle('notification:show', (_event, notification) => {
  mainWindow?.webContents.send('notification:show', notification);
});
ipcMain.handle('file:save-dialog', (_event, options) => dialog.showSaveDialog(mainWindow, options));
ipcMain.handle('file:open-dialog', (_event, options) => dialog.showOpenDialog(mainWindow, options));
ipcMain.handle('app:set-badge-count', (_event, count) => app.setBadgeCount(count));
ipcMain.handle('deeplink:register', () => app.setAsDefaultProtocolClient('chat'));
ipcMain.handle('updater:check-for-updates', () => autoUpdater.checkForUpdates());
ipcMain.handle('updater:install-update', () => autoUpdater.quitAndInstall());

autoUpdater.on('update-available', (info) => mainWindow?.webContents.send('updater:update-available', info));
