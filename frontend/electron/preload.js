const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('nexchat', {
  getVersion: () => ipcRenderer.invoke('app:get-version'),
  showNotification: (title, body, icon) => ipcRenderer.invoke('notification:show', { title, body, icon }),
  saveDialog: (options) => ipcRenderer.invoke('file:save-dialog', options),
  openDialog: (options) => ipcRenderer.invoke('file:open-dialog', options),
  setBadgeCount: (count) => ipcRenderer.invoke('app:set-badge-count', count),
  registerDeepLink: () => ipcRenderer.invoke('deeplink:register'),
  checkForUpdates: () => ipcRenderer.invoke('updater:check-for-updates'),
  installUpdate: () => ipcRenderer.invoke('updater:install-update'),
  onUpdateAvailable: (callback) => ipcRenderer.on('updater:update-available', (_event, info) => callback(info)),
  onDeepLink: (callback) => ipcRenderer.on('deeplink:url', (_event, url) => callback(url))
});
