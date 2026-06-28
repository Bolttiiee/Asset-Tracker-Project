import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { UserService } from '../../../app/services/user';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
    localStorage.setItem('token', 'jwt-token');
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('builds query string correctly for getAllUsers filters', () => {
    service.getAllUsers({ search: 'jane', department: 'IT', role: 'Admin', status: 'Pending' }).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/users?search=jane&department=IT&role=Admin&status=Pending');

    expect(request.request.method).toBe('GET');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    request.flush([]);
  });

  it('gets user by id', () => {
    service.getUserById(3).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/users/3');
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('posts to approve endpoint', () => {
    service.approveUser(8).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/users/8/approve');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush({ message: 'ok' });
  });

  it('posts rejection reason payload', () => {
    service.rejectUser(8, 'Insufficient details').subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/users/8/reject');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ rejectionReason: 'Insufficient details' });
    request.flush({ message: 'ok' });
  });

  it('updates status and role with correct payloads', () => {
    service.updateUserStatus(5, true).subscribe();
    service.updateUserRole(5, 'Asset Manager').subscribe();

    const statusRequest = httpMock.expectOne('https://localhost:7175/api/users/5/status');
    expect(statusRequest.request.method).toBe('PUT');
    expect(statusRequest.request.body).toEqual({ activate: true });
    statusRequest.flush({ message: 'ok' });

    const roleRequest = httpMock.expectOne('https://localhost:7175/api/users/5/role');
    expect(roleRequest.request.method).toBe('PUT');
    expect(roleRequest.request.body).toEqual({ newRole: 'Asset Manager' });
    roleRequest.flush({ message: 'ok' });
  });

  it('posts to reset password endpoint', () => {
    service.resetUserPassword(4).subscribe();

    const request = httpMock.expectOne('https://localhost:7175/api/users/4/reset-password');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush({ message: 'ok', temporaryPassword: 'Temp123!' });
  });
});




