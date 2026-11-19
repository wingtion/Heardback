import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { WeeklyCount } from './insights.models';
import { WeeklyChart } from './weekly-chart';

@Component({
  imports: [WeeklyChart],
  template: `<app-weekly-chart [weeks]="weeks()" />`,
})
class Host {
  readonly weeks = signal<WeeklyCount[]>([
    { weekStart: '2026-11-02', applications: 2 },
    { weekStart: '2026-11-09', applications: 0 },
    { weekStart: '2026-11-16', applications: 4 },
  ]);
}

describe('WeeklyChart', () => {
  function render(): HTMLElement {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('bars_OnePerWeek_TheLatestInTheAccent', () => {
    const element = render();
    const bars = Array.from(element.querySelectorAll('rect'));

    expect(bars).toHaveLength(3);
    expect(bars[2].getAttribute('class')).toContain('fill-accent');
    expect(bars[1].getAttribute('class')).toContain('fill-rule');
  });

  it('bars_HeightFollowsTheCountAndAnEmptyWeekKeepsAStub', () => {
    const heights = Array.from(render().querySelectorAll('rect'), (bar) =>
      Number(bar.getAttribute('height')),
    );

    expect(heights[2]).toBeGreaterThan(heights[0]);
    expect(heights[1]).toBeGreaterThan(0);
  });

  it('counts_ShownUnderTheBarsAndInATableForScreenReaders', () => {
    const element = render();

    expect(element.querySelector('rect title')?.textContent).toContain('2 applications');
    const rows = element.querySelectorAll('table.sr-only tr');
    expect(rows).toHaveLength(4);
    expect(rows[3].textContent).toContain('4');
  });
});
