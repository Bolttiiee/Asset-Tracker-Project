import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../app/services/auth';

import { RegisterComponent } from '../../../../app/pages/register/register';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let registerCalls: any[];

  beforeEach(async () => {
    registerCalls = [];

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        {
          provide: AuthService,
          useValue: {
            register: (payload: any) => {
              registerCalls.push(payload);
              return of({ message: 'ok' });
            },
            checkEmailAvailable: () => of({ available: true }),
            checkEmployeeIDAvailable: () => of({ available: true }),
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

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('password complexity validator rejects missing uppercase, number, or special character', () => {
    expect(component.passwordComplexityValidator({ value: 'lowercase1!' } as any)).toEqual({ complexity: true });
    expect(component.passwordComplexityValidator({ value: 'Password!' } as any)).toEqual({ complexity: true });
    expect(component.passwordComplexityValidator({ value: 'Password1' } as any)).toEqual({ complexity: true });
    expect(component.passwordComplexityValidator({ value: 'Password1!' } as any)).toBeNull();
  });

  it('passwordMatchValidator rejects mismatched passwords', () => {
    const mismatch = component.passwordMatchValidator({
      get: (name: string) => ({ value: name === 'password' ? 'Password1!' : 'Mismatch1!' }),
    } as any);

    expect(mismatch).toEqual({ passwordMismatch: true });
  });

  it('getPasswordStrength returns weak, medium, and strong states', () => {
    component.registerForm.get('password')?.setValue('weak');
    expect(component.getPasswordStrength().text).toBe('Weak');

    component.registerForm.get('password')?.setValue('Password');
    expect(component.getPasswordStrength().text).toBe('Medium');

    component.registerForm.get('password')?.setValue('Password1!');
    expect(component.getPasswordStrength().text).toBe('Strong');
  });

  it('does not submit when the form is invalid', () => {
    component.onSubmit();

    expect(registerCalls.length).toBe(0);
    expect(component.isSubmitted).toBe(true);
  });

  it('sets success state when registration succeeds', async () => {
    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@company.com',
      employeeID: 'EMP001',
      department: 'IT',
      phoneNumber: '+27821234567',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });

    // Wait for async validators to complete by waiting for form to be valid
    await new Promise(resolve => {
      const sub = component.registerForm.statusChanges.subscribe(() => {
        if (component.registerForm.valid) {
          sub.unsubscribe();
          resolve(null);
        }
      });
      // Timeout after 1 second in case validators don't complete
      setTimeout(() => {
        sub.unsubscribe();
        resolve(null);
      }, 1000);
    });

    component.onSubmit();

    expect(registerCalls.length).toBe(1);
    expect(component.isSuccess).toBe(true);
    expect(component.successMessage).toContain('awaiting administrator approval');
    expect(component.isSubmitting).toBe(false);
  });

  it('sets error message when registration fails', async () => {
    const authService = TestBed.inject(AuthService) as any;
    authService.register = () => throwError(() => ({ error: { message: 'Registration failed.' } }));

    component.registerForm.setValue({
      fullName: 'Jane Doe',
      email: 'jane@company.com',
      employeeID: 'EMP001',
      department: 'IT',
      phoneNumber: '+27821234567',
      password: 'Password1!',
      confirmPassword: 'Password1!',
    });

    // Wait for async validators to complete by waiting for form to be valid
    await new Promise(resolve => {
      const sub = component.registerForm.statusChanges.subscribe(() => {
        if (component.registerForm.valid) {
          sub.unsubscribe();
          resolve(null);
        }
      });
      // Timeout after 1 second in case validators don't complete
      setTimeout(() => {
        sub.unsubscribe();
        resolve(null);
      }, 1000);
    });

    component.onSubmit();

    expect(component.errorMessage).toBe('Registration failed.');
    expect(component.isSubmitting).toBe(false);
  });
});




