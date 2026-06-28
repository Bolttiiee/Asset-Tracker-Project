import { TestBed } from '@angular/core/testing';
import { CanActivateFn, Router } from '@angular/router';
import { roleGuard } from '../../../app/guards/role-guard';

describe('roleGuard', () => {
  let router: any;
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => roleGuard(...guardParameters));

  beforeEach(() => {
    router = { navigate: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        {
          provide: Router,
          useValue: router,
        },
      ],
    });
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });

  it('allows access when no roles are required', () => {
    const result = executeGuard({ data: {} } as any, {} as any);
    expect(result).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('allows access when user role matches allowed roles', () => {
    localStorage.setItem('userRole', 'Admin');

    const result = executeGuard({ data: { roles: ['Admin', 'Asset Manager'] } } as any, {} as any);
    expect(result).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('navigates to inventory and denies access when role does not match', () => {
    localStorage.setItem('userRole', 'Standard User');

    const result = executeGuard({ data: { roles: ['Admin'] } } as any, {} as any);
    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/inventory']);
  });

  it('falls back to parsed user profile role when userRole not in localStorage', () => {
    localStorage.setItem('user_profile', JSON.stringify({ role: 'Asset Manager' }));

    const result = executeGuard({ data: { roles: ['Asset Manager'] } } as any, {} as any);
    expect(result).toBe(true);
  });
});




