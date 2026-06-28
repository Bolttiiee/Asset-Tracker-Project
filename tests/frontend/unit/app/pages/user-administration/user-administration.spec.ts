import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { UserService } from '../../../../app/services/user';

import { UserAdministrationComponent } from '../../../../app/pages/user-administration/user-administration';

describe('UserAdministrationComponent', () => {
  let component: UserAdministrationComponent;
  let fixture: ComponentFixture<UserAdministrationComponent>;
  let getAllUsersCalls: any[];
  let approveCalls: number[];
  let rejectCalls: Array<{ id: number; reason: string }>;
  let statusCalls: Array<{ id: number; activate: boolean }>;
  let roleCalls: Array<{ id: number; role: string }>;
  let resetCalls: number[];

  beforeEach(async () => {
    getAllUsersCalls = [];
    approveCalls = [];
    rejectCalls = [];
    statusCalls = [];
    roleCalls = [];
    resetCalls = [];

    await TestBed.configureTestingModule({
      imports: [UserAdministrationComponent],
      providers: [
        {
          provide: UserService,
          useValue: {
            getAllUsers: (filters: any) => {
              getAllUsersCalls.push({ ...filters });
              return of([]);
            },
            getUserById: () => of({ userID: 1, fullName: 'Jane Doe', email: 'jane@company.com', department: 'IT', employeeID: 'EMP001', role: 'Standard User', accountStatus: 'Pending', registrationDate: '2026-06-01', currentCheckouts: [] }),
            approveUser: (id: number) => {
              approveCalls.push(id);
              return of({ message: 'ok' });
            },
            rejectUser: (id: number, reason: string) => {
              rejectCalls.push({ id, reason });
              return of({ message: 'ok' });
            },
            updateUserStatus: (id: number, activate: boolean) => {
              statusCalls.push({ id, activate });
              return of({ message: 'ok' });
            },
            updateUserRole: (id: number, role: string) => {
              roleCalls.push({ id, role });
              return of({ message: 'ok' });
            },
            resetUserPassword: (id: number) => {
              resetCalls.push(id);
              return of({ message: 'ok', temporaryPassword: 'Temp123!' });
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserAdministrationComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('initial load prioritizes pending users', () => {
    getAllUsersCalls = [];
    component.ngOnInit();

    expect(component.filters.status).toBe('Pending');
    expect(getAllUsersCalls.length).toBe(1);
    expect(getAllUsersCalls[0].status).toBe('Pending');
  });

  it('loadUsers populates users on success', () => {
    const users = [{ userID: 1, fullName: 'Jane Doe', email: 'jane@company.com', department: 'IT', employeeID: 'EMP001', role: 'Standard User', accountStatus: 'Pending', registrationDate: '2026-06-01' }];
    const service = TestBed.inject(UserService) as any;
    service.getAllUsers = () => of(users);

    component.loadUsers();

    expect(component.users.length).toBe(1);
    expect(component.loading).toBe(false);
  });

  it('loadUsers sets error message on failure', () => {
    const service = TestBed.inject(UserService) as any;
    service.getAllUsers = () => throwError(() => ({ error: { message: 'Failed to load users.' } }));

    component.loadUsers();

    expect(component.errorMessage).toBe('Failed to load users.');
    expect(component.loading).toBe(false);
  });

  it('approve flow calls approveUser service after confirmation', () => {
    const user = { userID: 2, fullName: 'John Doe', email: 'john@company.com' } as any;

    component.approveUser(user);
    component.confirmAction();

    expect(approveCalls).toEqual([2]);
    expect(component.busyUserId).toBeNull();
  });

  it('reject flow requires a non-empty reason', () => {
    const user = { userID: 3, fullName: 'John Doe', email: 'john@company.com' } as any;

    component.openRejectModal(user);
    component.confirmReject();

    expect(rejectCalls.length).toBe(0);
  });

  it('reject flow calls rejectUser with trimmed reason', () => {
    const user = { userID: 3, fullName: 'John Doe', email: 'john@company.com' } as any;

    component.openRejectModal(user);
    component.rejectReason = '  Missing details  ';
    component.confirmReject();

    expect(rejectCalls).toEqual([{ id: 3, reason: 'Missing details' }]);
    expect(component.rejectModalOpen).toBe(false);
  });

  it('activation toggle calls updateUserStatus with correct boolean', () => {
    const user = { userID: 4, fullName: 'Jane Doe', email: 'jane@company.com', accountStatus: 'Pending' } as any;

    component.toggleActivation(user);
    component.confirmAction();

    expect(statusCalls).toEqual([{ id: 4, activate: true }]);
  });

  it('role change calls updateUserRole when new role differs', () => {
    const user = { userID: 5, fullName: 'Jane Doe', role: 'Standard User' } as any;

    component.changeRole(user, 'Admin');
    component.confirmAction();

    expect(roleCalls).toEqual([{ id: 5, role: 'Admin' }]);
  });

  it('reset password calls service and shows temporary password', () => {
    const originalSetTimeout = globalThis.setTimeout;
    (globalThis as any).setTimeout = () => 0;

    try {
      const user = { userID: 6, fullName: 'Jane Doe' } as any;
      component.resetPassword(user);
      component.confirmAction();

      expect(resetCalls).toEqual([6]);
      expect(component.actionMessage).toContain('Temp123!');
    } finally {
      globalThis.setTimeout = originalSetTimeout;
    }
  });
});




