import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, throwError } from 'rxjs';
import { AuthService } from '../../../../app/services/auth';

import { LoginComponent } from '../../../../app/pages/login/login';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let loginCalls: any[];
  let forgotPasswordCalls: string[];
  let routerNavigations: string[][];
  let loginResponse$: Subject<any>;
  let forgotPasswordResponse$: Subject<any>;

  beforeEach(async () => {
    loginCalls = [];
    forgotPasswordCalls = [];
    routerNavigations = [];
    loginResponse$ = new Subject<any>();
    forgotPasswordResponse$ = new Subject<any>();

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        {
          provide: AuthService,
          useValue: {
            login: (payload: any) => {
              loginCalls.push(payload);
              return loginResponse$.asObservable();
            },
            forgotPassword: (email: string) => {
              forgotPasswordCalls.push(email);
              return forgotPasswordResponse$.asObservable();
            },
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
              queryParams: {},
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('does not submit login when the form is invalid', () => {
    component.onSubmit();

    expect(loginCalls.length).toBe(0);
    expect(component.isSubmitted()).toBe(true);
  });

  it('navigates to inventory after successful login', () => {
    component.loginForm.setValue({ email: 'user@company.com', password: 'Secret123!' });

    component.onSubmit();
    loginResponse$.next({ token: 'jwt' });
    loginResponse$.complete();

    expect(loginCalls.length).toBe(1);
    expect(routerNavigations).toEqual([['/inventory']]);
    expect(component.isSubmitting()).toBe(false);
  });

  it('sets errorMessage when login fails', () => {
    loginResponse$ = new Subject<any>();
    const authService = TestBed.inject(AuthService) as any;
    authService.login = () => throwError(() => ({ error: { message: 'Invalid credentials.' } }));

    component.loginForm.setValue({ email: 'user@company.com', password: 'wrong' });
    component.onSubmit();

    expect(component.errorMessage()).toBe('Invalid credentials.');
    expect(component.isSubmitting()).toBe(false);
  });

  it('opens and closes the forgot password panel', () => {
    component.triggerForgotPassword();
    expect(component.showForgotPasswordBox()).toBe(true);

    component.cancelForgotPassword();
    expect(component.showForgotPasswordBox()).toBe(false);
  });

  it('does not call forgotPassword when forgot password form is invalid', () => {
    component.triggerForgotPassword();
    component.forgotPasswordForm.setValue({ email: 'not-an-email' });

    component.submitForgotPassword();

    expect(forgotPasswordCalls.length).toBe(0);
  });

  it('sets confirmation message on forgot password success', () => {
    component.triggerForgotPassword();
    component.forgotPasswordForm.setValue({ email: 'user@company.com' });

    component.submitForgotPassword();
    forgotPasswordResponse$.next({ message: 'Reset email sent.' });
    forgotPasswordResponse$.complete();

    expect(forgotPasswordCalls).toEqual(['user@company.com']);
    expect(component.forgotPasswordMessage()).toBe('Reset email sent.');
    expect(component.isForgotSubmitting()).toBe(false);
  });

  it('sets error message on forgot password failure', () => {
    forgotPasswordResponse$ = new Subject<any>();
    const authService = TestBed.inject(AuthService) as any;
    authService.forgotPassword = () => throwError(() => ({ error: { message: 'Dispatch failed.' } }));

    component.triggerForgotPassword();
    component.forgotPasswordForm.setValue({ email: 'user@company.com' });
    component.submitForgotPassword();

    expect(component.forgotPasswordError()).toBe('Dispatch failed.');
    expect(component.isForgotSubmitting()).toBe(false);
  });
});
