import { bootstrapApplication } from '@angular/platform-browser';
import { registerLocaleData } from '@angular/common';
import localeEnZA from '@angular/common/locales/en-ZA';
import { appConfig } from './app/app.config';
import { App } from './app/app';

registerLocaleData(localeEnZA);

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
