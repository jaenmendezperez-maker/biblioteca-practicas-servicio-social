if (!(Promise as any).try) {
  (Promise as any).try = function (fn: () => any) {
    return new Promise((resolve) => resolve(fn()));
  };
}

import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));