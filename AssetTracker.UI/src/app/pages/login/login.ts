import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  loginForm: FormGroup;
  forgotPasswordForm: FormGroup;
  isSubmitted = signal(false);
  isSubmitting = signal(false);
  errorMessage = signal('');
  showForgotPasswordBox = signal(false);
  forgotPasswordMessage = signal('');
  forgotPasswordError = signal('');
  isForgotSubmitting = signal(false);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });

    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });
  }

  private errorTimeout: any;
  private forgotSuccessTimeout: any;
  private forgotErrorTimeout: any;

  onSubmit() {
    this.isSubmitted.set(true);
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
    this.errorMessage.set('');

    if (this.loginForm.invalid) {
      return;
    }

    this.isSubmitting.set(true);

    this.authService.login(this.loginForm.getRawValue()).pipe(
      finalize(() => this.isSubmitting.set(false))
    ).subscribe({
      next: (res) => {
        if (res?.mustChangePassword || res?.user?.mustChangePassword) {
          this.router.navigate(['/reset-password'], { queryParams: { forced: 'true' } });
        } else {
          this.router.navigate(['/inventory']);
        }
      },
      error: (err) => {
        this.errorMessage.set(
          err?.error?.message || err?.message || 'Unable to reach the authentication service.'
        );
        this.errorTimeout = setTimeout(() => {
          this.errorMessage.set('');
        }, 4000);
      }
    });
  }

  triggerForgotPassword(): void {
    this.showForgotPasswordBox.set(true);
    if (this.forgotSuccessTimeout) clearTimeout(this.forgotSuccessTimeout);
    if (this.forgotErrorTimeout) clearTimeout(this.forgotErrorTimeout);
    this.forgotPasswordMessage.set('');
    this.forgotPasswordError.set('');
  }

  cancelForgotPassword(): void {
    this.showForgotPasswordBox.set(false);
    this.isForgotSubmitting.set(false);
    this.forgotPasswordForm.reset();
    if (this.forgotSuccessTimeout) clearTimeout(this.forgotSuccessTimeout);
    if (this.forgotErrorTimeout) clearTimeout(this.forgotErrorTimeout);
    this.forgotPasswordMessage.set('');
    this.forgotPasswordError.set('');
  }

  submitForgotPassword(): void {
    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      return;
    }

    const email = String(this.forgotPasswordForm.value.email || '').trim();
    this.isForgotSubmitting.set(true);
    if (this.forgotSuccessTimeout) clearTimeout(this.forgotSuccessTimeout);
    if (this.forgotErrorTimeout) clearTimeout(this.forgotErrorTimeout);
    this.forgotPasswordMessage.set('');
    this.forgotPasswordError.set('');

    this.authService.forgotPassword(email).pipe(
      finalize(() => this.isForgotSubmitting.set(false))
    ).subscribe({
      next: (res) => {
        this.forgotPasswordMessage.set(res?.message || 'Reset instructions have been sent if the account exists.');
        this.forgotSuccessTimeout = setTimeout(() => {
          this.forgotPasswordMessage.set('');
        }, 4000);
      },
      error: (err) => {
        this.forgotPasswordError.set(err?.error?.message || 'Failed to dispatch reset sequence.');
        this.forgotErrorTimeout = setTimeout(() => {
          this.forgotPasswordError.set('');
        }, 4000);
      }
    });
  }
}