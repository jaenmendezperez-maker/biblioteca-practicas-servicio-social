import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Libro } from '../../../core/models/libro.model';
import { LibroService } from '../../../core/services/libro.service';
import { AuthService } from '../../../core/services/auth.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { SidebarService } from '../../../core/services/sidebar.service';


@Component({
  selector: 'app-detalle-libro',
  standalone: true,
  imports: [CommonModule, RouterLink, SidebarComponent],
  templateUrl: './detalle-libro.html'
})
export class DetalleLibroComponent implements OnInit {
  libro: Libro | null = null;
  cargando = true;
  error = '';
  tabActiva: 'sinopsis' | 'contenido' | 'citas' = 'sinopsis';
  mostrarVistaPrevia = false;

  private coloresPortada = [
    'bg-marino-700',
    'bg-ambar-600',
    'bg-marino-900',
    'bg-marino-400',
  ];

  colorPortada(id: number): string {
    return this.coloresPortada[id % this.coloresPortada.length];
  }

  constructor(
    private route: ActivatedRoute,
    private libroService: LibroService,
    public authService: AuthService,
    public sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error = 'Libro no encontrado.';
      this.cargando = false;
      return;
    }

    this.libroService.getLibro(id).subscribe({
      next: (data) => {
        this.libro = data;
        this.cargando = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudo cargar el libro.';
        this.cargando = false;
      }
    });
  }

  cambiarTab(tab: 'sinopsis' | 'contenido' | 'citas'): void {
    this.tabActiva = tab;
  }

  iniciales(titulo: string): string {
    return titulo
      .split(' ')
      .filter(w => w.length > 0)
      .slice(0, 2)
      .map(w => w[0].toUpperCase())
      .join('');
  }

  urlArchivo(ruta: string): string {
    return `http://localhost:5250${ruta}`;
  }
  
  descargarPdf(): void {
    if (!this.libro) return;
    const nombre = `${this.libro.titulo}.pdf`;
    this.libroService.descargarArchivo(this.libro.rutaArchivo, nombre);
  }

  abrirVistaPrevia(): void {
    this.mostrarVistaPrevia = true;
  }

  cerrarVistaPrevia(): void {
    this.mostrarVistaPrevia = false;
  }
}