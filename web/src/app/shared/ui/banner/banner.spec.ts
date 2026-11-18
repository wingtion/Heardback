import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Banner } from './banner';

@Component({
  imports: [Banner],
  template: `
    <app-banner kind="warning" heading="Plain warning">Body</app-banner>
    <app-banner kind="warning" icon="refresh" alert heading="Changed elsewhere">Body</app-banner>
  `,
})
class BannerHost {}

describe('Banner', () => {
  function banners(): HTMLElement[] {
    const fixture = TestBed.createComponent(BannerHost);
    fixture.detectChanges();
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('app-banner'));
  }

  function iconPaths(banner: HTMLElement): string[] {
    return Array.from(banner.querySelectorAll('path')).map((path) => path.getAttribute('d') ?? '');
  }

  it('render_Warning_IsAStatusWithTheClockIcon', () => {
    const [plain] = banners();

    expect(plain.getAttribute('role')).toBe('status');
    expect(iconPaths(plain)).toContain('M8 4.8V8l2.2 1.5');
  });

  it('render_IconAndAlert_ReplaceTheIconAndRole', () => {
    const [, changed] = banners();

    expect(changed.getAttribute('role')).toBe('alert');
    expect(iconPaths(changed)).toContain('M13 2.5v2.6h-2.6');
  });
});
