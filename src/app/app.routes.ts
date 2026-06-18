import { Routes } from '@angular/router';
import { RegisterComponent } from './pages/register/register';
import { LoginComponent } from './pages/login/login';
import { InventoryGridComponent } from './components/inventory-grid/inventory-grid';
import { AuditTimelineComponent } from './components/audit-timeline/audit-timeline';
import { MyAssetsComponent } from './pages/my-assets/my-assets';
import { ReportsComponent } from './pages/reports/reports';
import { UserAdministrationComponent } from './pages/user-administration/user-administration';
import { ResetPasswordComponent } from './components/reset-password/reset-password';
import { authGuard } from './guards/auth-guard';
import { roleGuard } from './guards/role-guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'inventory', component: InventoryGridComponent, canActivate: [authGuard] },
  { path: 'audit-log', component: AuditTimelineComponent, canActivate: [authGuard, roleGuard], data: { roles: ['Admin'] } },
  { path: 'my-assets', component: MyAssetsComponent, canActivate: [authGuard] },
  { path: 'reports', component: ReportsComponent, canActivate: [authGuard, roleGuard], data: { roles: ['Admin', 'Asset Manager'] } },
  { path: 'user-administration', component: UserAdministrationComponent, canActivate: [authGuard, roleGuard], data: { roles: ['Admin'] } },
  { path: 'dashboard', redirectTo: 'inventory', pathMatch: 'full' },
  { path: '', redirectTo: 'login', pathMatch: 'full' } // Boot straight to Login screen now
];