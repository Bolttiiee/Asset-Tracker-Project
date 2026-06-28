import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { AuthService } from '../../../app/services/auth';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('stores tokens, profile, and nested role on login success', () => {
    service.login({ email: 'user@company.com', password: 'Secret123!' }).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/auth/login');
    expect(request.request.method).toBe('POST');

    request.flush({
      token: 'jwt-token',
      user: {
        fullName: 'User One',
        email: 'user@company.com',
        role: 'Admin',
      },
    });

    expect(localStorage.getItem('token')).toBe('jwt-token');
    expect(localStorage.getItem('asset_tracker_token')).toBe('jwt-token');
    expect(localStorage.getItem('userRole')).toBe('Admin');
    expect(localStorage.getItem('user_profile')).toContain('user@company.com');
  });

  it('falls back to top-level role when nested user role is missing', () => {
    service.login({ email: 'user@company.com', password: 'Secret123!' }).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/auth/login');
    request.flush({
      token: 'jwt-token',
      role: 'Asset Manager',
      user: {
        fullName: 'User One',
        email: 'user@company.com',
      },
    });

    expect(localStorage.getItem('userRole')).toBe('Asset Manager');
  });

  it('clears all auth storage keys on logout', () => {
    localStorage.setItem('token', 'a');
    localStorage.setItem('asset_tracker_token', 'b');
    localStorage.setItem('userRole', 'Admin');
    localStorage.setItem('user_profile', '{}');

    service.logout();

    expect(localStorage.getItem('token')).toBeNull();
    expect(localStorage.getItem('asset_tracker_token')).toBeNull();
    expect(localStorage.getItem('userRole')).toBeNull();
    expect(localStorage.getItem('user_profile')).toBeNull();
  });

  it('reports logged in when either token key exists', () => {
    expect(service.isLoggedIn()).toBe(false);

    localStorage.setItem('asset_tracker_token', 'jwt-token');
    expect(service.isLoggedIn()).toBe(true);
  });

  it('posts email payload for forgot password', () => {
    service.forgotPassword('user@company.com').subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/auth/forgot-password');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'user@company.com' });
    request.flush({ message: 'ok' });
  });

  it('posts token and new password for reset password', () => {
    service.resetPassword('reset-token', 'NewPass123!').subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/auth/reset-password');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ token: 'reset-token', newPassword: 'NewPass123!' });
    request.flush({ message: 'ok' });
  });
});




