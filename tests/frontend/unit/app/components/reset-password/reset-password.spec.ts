import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../../app/services/auth';

import { ResetPasswordComponent } from '../../../../app/components/reset-password/reset-password';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let resetCalls: Array<{ token: string; newPassword: string }>;
  let resetResponse$: Subject<any>;
  let routerNavigations: string[][];

  beforeEach(async () => {
    resetCalls = [];
    resetResponse$ = new Subject<any>();
    routerNavigations = [];

    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        {
          provide: AuthService,
          useValue: {
            resetPassword: (token: string, newPassword: string) => {
              resetCalls.push({ token, newPassword });
              return resetResponse$.asObservable();
            },
            changePassword: () => of({ message: 'Password updated successfully.' }),
          },
        },
        {
          provide: Router,
          useValue: {
            navigate: (commands: string[]) => {
              routerNavigations.push(commands);
              return Promise.resolve(true);
            },
          },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParams: { token: 'test-token' },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('reads token from query params on init', () => {
    expect(component.token).toBe('test-token');
  });

  it('sets an error when the token is missing', async () => {
    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        {
          provide: AuthService,
          useValue: {
            resetPassword: () => resetResponse$.asObservable(),
            changePassword: () => of({ message: 'ok' }),
          },
        },
        {
          provide: Router,
          useValue: {
            navigate: () => Promise.resolve(true),
          },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParams: {},
            },
          },
        },
      ],
    }).compileComponents();

    const missingTokenFixture = TestBed.createComponent(ResetPasswordComponent);
    const missingTokenComponent = missingTokenFixture.componentInstance;
    await missingTokenFixture.whenStable();

    expect(missingTokenComponent.errorMessage).toBe('Security violation: Missing verification token. Request a new link.');
  });

  it('does not submit reset request when password form is invalid', () => {
    component.submitNewPassword();

    expect(resetCalls.length).toBe(0);
  });

  it('sets success message on successful password reset', () => {
    const originalSetTimeout = globalThis.setTimeout;
    const scheduledCallbacks: Array<{ callback: Function; delay?: number }> = [];
    (globalThis as any).setTimeout = (callback: Function, delay?: number) => {
      scheduledCallbacks.push({ callback, delay });
      return 0;
    };

    try {
      component.resetForm.setValue({ password: 'NewPass123!', confirmPassword: 'NewPass123!' });
      component.submitNewPassword();
      resetResponse$.next({ message: 'Password updated successfully.' });
      resetResponse$.complete();

      expect(resetCalls).toEqual([{ token: 'test-token', newPassword: 'NewPass123!' }]);
      expect(component.successMessage).toBe('Password updated successfully.');
      expect(scheduledCallbacks.some((timer) => timer.delay === 3000)).toBe(true);
      expect(routerNavigations).toEqual([]);
    } finally {
      globalThis.setTimeout = originalSetTimeout;
    }
  });

  it('sets backend error message on reset failure', () => {
    resetResponse$ = new Subject<any>();
    const authService = TestBed.inject(AuthService) as any;
    authService.resetPassword = () => throwError(() => ({ error: { message: 'Token expired.' } }));

    component.resetForm.setValue({ password: 'NewPass123!', confirmPassword: 'NewPass123!' });
    component.submitNewPassword();

    expect(component.errorMessage).toBe('Token expired.');
  });

  it('forced mode: calls changePassword instead of resetPassword', async () => {
    await TestBed.resetTestingModule();
    const changeCalls: string[] = [];

    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        {
          provide: AuthService,
          useValue: {
            resetPassword: () => of({}),
            changePassword: (newPassword: string) => {
              changeCalls.push(newPassword);
              return of({ message: 'Password updated.' });
            },
          },
        },
        {
          provide: Router,
          useValue: { navigate: () => Promise.resolve(true) },
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParams: { forced: 'true' } } },
        },
      ],
    }).compileComponents();

    const forcedFixture = TestBed.createComponent(ResetPasswordComponent);
    const forcedComponent = forcedFixture.componentInstance;
    await forcedFixture.whenStable();

    expect(forcedComponent.forced).toBe(true);
    expect(forcedComponent.errorMessage).toBe('');

    forcedComponent.resetForm.setValue({ password: 'NewPass123!', confirmPassword: 'NewPass123!' });
    forcedComponent.submitNewPassword();

    expect(changeCalls).toEqual(['NewPass123!']);
    expect(forcedComponent.successMessage).toContain('Password updated');
  });
});




