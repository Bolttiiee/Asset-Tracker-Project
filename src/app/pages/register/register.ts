import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth';
import { finalize, switchMap, map, catchError, timer } from 'rxjs';
import { of } from 'rxjs';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent implements OnInit {
  registerForm: FormGroup;
  isSubmitted = false;
  isSuccess = false;
  successMessage = '';
  errorMessage = '';
  isSubmitting = false;
  showPasswordCriteria = false;

  constructor(private fb: FormBuilder, private authService: AuthService, private cdr: ChangeDetectorRef) {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)], this.emailUniqueValidator()],
      employeeID: ['', [Validators.required, Validators.maxLength(50)], this.employeeIDUniqueValidator()],
      department: ['', [Validators.required, Validators.maxLength(100)]],
      phoneNumber: [''],
      password: ['', [Validators.required, Validators.minLength(8), this.passwordComplexityValidator]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    // Focus on password field to show criteria
    this.registerForm.get('password')?.valueChanges.subscribe(() => {
      this.showPasswordCriteria = !!this.registerForm.get('password')?.value;
      this.cdr.markForCheck();
    });

    // Keep async-validator UI states (e.g. "(checking...)") in sync without zone.js.
    this.registerForm.statusChanges.subscribe(() => this.cdr.markForCheck());
  }

  // Async validator: Check email uniqueness (debounced 500ms)
  emailUniqueValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      const value = control.value;
      if (!value || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
        return of(null);
      }
      return timer(500).pipe(
        switchMap(() => this.authService.checkEmailAvailable(value)),
        map(res => res.available ? null : { emailTaken: true }),
        catchError(() => of(null))
      );
    };
  }

  // Async validator: Check employee ID uniqueness (debounced 500ms)
  employeeIDUniqueValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      const value = control.value;
      if (!value) return of(null);
      return timer(500).pipe(
        switchMap(() => this.authService.checkEmployeeIDAvailable(value)),
        map(res => res.available ? null : { employeeIDTaken: true }),
        catchError(() => of(null))
      );
    };
  }

  // Password complexity validator: 8+ chars, 1 uppercase, 1 number, 1 special
  passwordComplexityValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value || '';
    if (!value) return null;
    
    const hasUpperCase = /[A-Z]/.test(value);
    const hasNumber = /[0-9]/.test(value);
    const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(value);
    
    if (!hasUpperCase || !hasNumber || !hasSpecial) {
      return { complexity: true };
    }
    return null;
  }

  // Password match validator
  passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  getPasswordStrength(): { text: string; color: string; percent: number } {
    const value = this.registerForm.get('password')?.value || '';
    if (!value) return { text: 'Empty', color: '#ccc', percent: 0 };
    
    let strength = 0;
    if (value.length >= 8) strength += 25;
    if (/[A-Z]/.test(value)) strength += 25;
    if (/[0-9]/.test(value)) strength += 25;
    if (/[!@#$%^&*(),.?":{}|<>]/.test(value)) strength += 25;
    
    let text = 'Weak';
    let color = '#dc3545';
    if (strength >= 75) {
      text = 'Strong';
      color = '#319795';
    } else if (strength >= 50) {
      text = 'Medium';
      color = '#3a78a2';
    }
    
    return { text, color, percent: strength };
  }

  getFieldError(fieldName: string): string {
    const field = this.registerForm.get(fieldName);
    if (!field) return '';

    const shouldShow = this.isSubmitted || field.dirty || field.touched;
    if (!shouldShow) return '';

    const errors = field.errors;
    if (errors) {
      if (errors['required']) return `${this.formatFieldName(fieldName)} is required.`;
      if (errors['email']) return 'Please enter a valid email address.';
      if (errors['maxlength']) return `${this.formatFieldName(fieldName)} is too long.`;
      if (errors['minlength']) return `${this.formatFieldName(fieldName)} must be at least 8 characters.`;
      if (errors['complexity']) return 'Password must include uppercase, number, and special character.';
      if (errors['emailTaken']) return 'This email is already registered.';
      if (errors['employeeIDTaken']) return 'This Employee ID is already registered.';
    }

    // passwordMismatch is a group-level error — check it for the confirmPassword field
    if (fieldName === 'confirmPassword' && this.registerForm.errors?.['passwordMismatch']) {
      return 'Passwords do not match.';
    }

    return '';
  }

  formatFieldName(name: string): string {
    return name.replace(/([A-Z])/g, ' $1').trim().split(' ').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ');
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    if (!field) return false;
    const showError = !!(field.invalid && (field.dirty || field.touched || this.isSubmitted));
    // confirmPassword is also invalid when the group has a passwordMismatch error
    if (fieldName === 'confirmPassword' && this.registerForm.errors?.['passwordMismatch']) {
      return !!(field.dirty || field.touched || this.isSubmitted);
    }
    return showError;
  }

  hasUpperCase(): boolean {
    return /[A-Z]/.test(this.registerForm.get('password')?.value || '');
  }

  hasNumber(): boolean {
    return /[0-9]/.test(this.registerForm.get('password')?.value || '');
  }

  hasSpecialChar(): boolean {
    return /[!@#$%^&*(),.?":{}|<>]/.test(this.registerForm.get('password')?.value || '');
  }

  onSubmit(): void {
    this.isSubmitted = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.registerForm.invalid || this.registerForm.pending) {
      return;
    }

    this.isSubmitting = true;

    this.authService.register(this.registerForm.value).pipe(
      finalize(() => {
        this.isSubmitting = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (res) => {
        this.isSuccess = true;
        this.successMessage = 'Your account is awaiting administrator approval. You will receive a confirmation email once approved.';
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = err?.status === 0
          ? 'Cannot reach the server. Please check your connection and try again.'
          : err?.error?.message || 'An error occurred during registration.';
        this.cdr.markForCheck();
      }
    });
  }
}